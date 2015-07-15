using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace DataTestLoader
{
    public class DatabaseScriptManager : BaseClass
    {

        private string servicePath;
        private string hostName;
        private string psqlExe;
        private string createdbExe;
        private string pgdumpExe;

        private ConnectionParser dbSource;
        private ConnectionParser dbTest;

        public const string SQL_CreateExtensionPostgis = @"CREATE EXTENSION postgis;";

        public DatabaseScriptManager(bool refreshSchema, bool initDatabase, bool loadJsonData)
        {
            this.refreshSchema = refreshSchema;
            this.initDatabase = initDatabase;
            this.loadJsonData = loadJsonData;

            CheckValidSettings();
        }

        public void InitDatabase()
        {

            DropAllConnections();
            DropDatabase();
            CreateDatabase();
            CreateExtensionPostgis();
            ApplySchemaDatabase();
            RunScriptsPreData();
            RunScriptsFillData();

            Debug.WriteLine(string.Format("\r\nINFO: Init database {0} successfull.  OK!", dbTest.Database));

        }

        public bool RefreshDatabaseSchema()
        {
            bool weCanProceed;

            if (this.refreshSchema)
                weCanProceed = ExportSchemaFromSourceDatabase();
            else
            {
                // reuse existing schema
                weCanProceed = CheckExistSchemaFile();
                Debug.WriteLine(string.Format("\r\nINFO: Reusing database schema {0}", FileSchemaFullName));
            }

            return weCanProceed;
        }

        private bool CheckValidSettings()
        {
            try
            {
                if (!Directory.Exists(FolderSchema))
                    Directory.CreateDirectory(FolderSchema);

                string logPath = Path.Combine(FolderSchema, "log");

                if (Directory.Exists(logPath))
                    Directory.Delete(logPath, true); // clear log smart mode

                Directory.CreateDirectory(logPath);

                dbSource = new ConnectionParser(ConnectionStringDBSource);
                if (dbSource == null)
                    throw new ApplicationException("Database source is invalid. ");

                dbTest = new ConnectionParser(ConnectionStringDBTest);
                if (dbTest == null)
                    throw new ApplicationException("Database test is invalid. ");
                if (!dbTest.Database.EndsWith("_test"))
                    throw new ApplicationException("The name of database test must be contain the word '_test' at end of name.");

                hostName = GetMachineNameFromIPAddress(dbSource.Server);
                if (string.IsNullOrEmpty(hostName))
                    throw new ApplicationException(string.Format("Host {0} is not reachable. ", dbSource.Server));

                servicePath = GetServicePath("postgresql");
                if (string.IsNullOrEmpty(servicePath))
                    throw new ApplicationException(string.Format("Postgres DB service is not installed on server {0}. ", dbSource.Server));

                psqlExe = Path.Combine(this.servicePath, "psql.exe");
                if (!File.Exists(psqlExe))
                    throw new FileNotFoundException(string.Format("Not found {0}", psqlExe));

                createdbExe = Path.Combine(this.servicePath, "createdb.exe");
                if (!File.Exists(createdbExe))
                    throw new FileNotFoundException(string.Format("Not found {0}", createdbExe));

                pgdumpExe = Path.Combine(this.servicePath, "pg_dump.exe");
                if (!File.Exists(pgdumpExe))
                    throw new FileNotFoundException(string.Format("Not found {0}", pgdumpExe));

                // the assembly model is required only when it is required to load data from json
                if (this.loadJsonData)
                {
                    if (!File.Exists(Path.Combine(AssemblyDirectory, AssemblyModel + ".dll")))
                        throw new FileNotFoundException(string.Format("Assembly Model {0} was not found on {1}", AssemblyModel, AssemblyDirectory));
                }

                Debug.WriteLine("\r\nINFO: All settings are valid. DataTestLoader will be run.\r\n");

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool ExportSchemaFromSourceDatabase()
        {

            try
            {
                // use pg_dump command

                this.FileSchemaName = string.Empty;
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                string file = String.Format("{0}-{1}-{2}.sql", hostName, dbSource.Database, timestamp);

                string fullPath = Path.Combine(FolderSchema, file);

                // http://www.postgresonline.com/special_feature.php?sf_name=postgresql90_pg_dumprestore_cheatsheet&outputformat=html
                // http://www.commandprompt.com/ppbook/x17860
                string arguments = String.Format(@" --host {0} --port {1} --username {2} --schema-only --verbose --no-owner --role {2} --encoding UTF8 --inserts --column-inserts --no-privileges --no-tablespaces  --file {3} --dbname {4}", dbSource.Server, dbSource.Port, dbSource.Username, fullPath, dbSource.Database);

                Debug.WriteLine(pgdumpExe + arguments);

                System.Environment.SetEnvironmentVariable("PGPASSWORD", dbSource.Password);

                ProcessStartInfo processInfo = CreateProcessInfo(pgdumpExe, arguments);

                string errorFile = Path.Combine(FolderSchema, @"log\DTL-CreateSchema.log");

                Debug.WriteLine(string.Format("INFO: Waiting please, retrieving DB schema from {0} may take up two minutes ", dbSource.Server));

                RunProcess(processInfo, errorFile);

                this.FileSchemaName = fullPath;

                if (this.FileSchemaName.Length > 0)
                    Debug.WriteLine(string.Format("INFO: Created schema {0}   OK!", this.FileSchemaName));
                else
                {
                    Debug.WriteLine(string.Format("\r\nErrors on creation schema. Aborted"));
                    throw new ApplicationException(string.Format("Errors on creation schema {0}. ", this.FileSchemaName));
                }

                CheckExistSchemaFile();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private bool CheckExistSchemaFile()
        {
            this.FileSchemaFullName = Path.Combine(FolderSchema, this.FileSchemaName);

            if (!File.Exists(this.FileSchemaFullName))
                throw new FileNotFoundException(string.Format("File schema {0} not found.", this.FileSchemaFullName));

            return true;
        }

        private void DropAllConnections()
        {
            try
            {
                // valid for v.9.2 and above (else use procpid in substitution of pid)
                string psqlCmd = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE pid <> pg_backend_pid()";

                // use psql command
                string arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --command ""{3}"" ", dbTest.Server, dbTest.Port, dbTest.Username, psqlCmd, dbTest.Database);

                ProcessStartInfo processInfo = CreateProcessInfo(psqlExe, arguments);

                string errorFile = Path.Combine(FolderSchema, @"log\DTL-DropAllConnections.log");

                Debug.WriteLine("\r\n" + psqlExe + arguments);

                Debug.Write(string.Format("INFO: Dropping all connections from database {0} ...", dbTest.Database));

                RunProcess(processInfo, errorFile);

                Debug.WriteLine("   OK!");

            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private void CreateExtensionPostgis()
        {
            try
            {
                string psqlCmd = SQL_CreateExtensionPostgis;

                // use psql command
                string arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --command ""{3}"" ", dbTest.Server, dbTest.Port, dbTest.Username, psqlCmd, dbTest.Database);

                ProcessStartInfo processInfo = CreateProcessInfo(psqlExe, arguments);

                string errorFile = Path.Combine(FolderSchema, @"log\DTL-CreateExtensionPostgis.log");

                Debug.WriteLine("\r\n" + psqlExe + arguments);

                Debug.Write(string.Format("INFO: Creating extension Postgis ...", dbTest.Database));

                RunProcess(processInfo, errorFile);

                Debug.WriteLine("   OK!");

            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private void DropDatabase()
        {
            try
            {
                string dropdbExe = Path.Combine(this.servicePath, "dropdb.exe");

                if (!File.Exists(dropdbExe))
                    throw new FileNotFoundException(string.Format("Not found {0}", dropdbExe));

                // Step 2 : drop database
                string arguments = String.Format(@" --host {0} --port {1} --username {2} {3}", dbTest.Server, dbTest.Port, dbTest.Username, dbTest.Database);

                ProcessStartInfo processInfo = CreateProcessInfo(dropdbExe, arguments);

                string errorFile = Path.Combine(FolderSchema, @"log\DTL-DropDatabase.log");

                Debug.WriteLine("\r\n" + dropdbExe + arguments);

                Debug.Write(string.Format("INFO: Dropping database {0}...", dbTest.Database));

                RunProcess(processInfo, errorFile);

                Debug.WriteLine("   OK!");

            }
            catch (Exception ex)
            {
                // only debug message
                Debug.WriteLine(ex.Message);
            }

        }

        private void CreateDatabase()
        {
            try
            {
                // use createDb command

                string arguments = String.Format(@" --host {0} --port {1} --username {2} {3}", dbTest.Server, dbTest.Port, dbTest.Username, dbTest.Database);

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = createdbExe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                string errorFile = Path.Combine(FolderSchema, @"log\DTL-CreateDatabase.log");

                Debug.WriteLine("\r\n" + createdbExe + arguments);

                Debug.Write(string.Format("INFO: Creating database {0}...", dbTest.Database));

                RunProcess(processInfo, errorFile);

                Debug.WriteLine("   OK!");

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

        }

        private static void RunProcess(ProcessStartInfo processInfo, string errorFile)
        {
            using (Process process = Process.Start(processInfo))
            {
                using (StreamReader reader = process.StandardError)
                {
                    StreamWriter sw = new StreamWriter(errorFile);
                    sw.WriteLine(reader.ReadToEnd());
                    sw.Close();
                }
            }
        }

        private void ApplySchemaDatabase()
        {
            try
            {
                // use psql command

                string arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --file ""{3}"" ", dbTest.Server, dbTest.Port, dbTest.Username, FileSchemaFullName, dbTest.Database);

                ProcessStartInfo processInfo = CreateProcessInfo(psqlExe, arguments);

                string errorFile = Path.Combine(FolderSchema, @"log\DTL-ApplySchema.log");

                Debug.WriteLine("\r\n" + psqlExe + arguments);

                Debug.Write("INFO: Applying database schema ...");

                RunProcess(processInfo, errorFile);

                Debug.WriteLine("   OK!");

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

        }

        private ProcessStartInfo CreateProcessInfo(string exeName, string arguments)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = exeName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            return processInfo;
        }

        private void RunScriptsPreData()
        {
            // ------------------------
            // 1. PRE-DATA section (result redirect to output file)
            // ------------------------
            string scriptName, arguments, outFile;
            bool result;

            // Step 1.1 - Create script for ADD database constraints - 
            scriptName = Path.Combine(AssemblyDirectory, @"DatabaseScripts\99. DB List constraints add.sql");
            if (!File.Exists(scriptName))
                throw new FileNotFoundException(string.Format("Not found {0}", scriptName));


            outFile = Path.Combine(FolderSchema, @"log\DTL-ListConstraintsAdd.log");
            arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --file ""{3}"" --output {5}",
                dbTest.Server, dbTest.Port, dbTest.Username, scriptName, dbTest.Database, outFile);
            // The result of this run is the file DTL-AddConstraints.inp
            result = ExecPsqlCommand(arguments);


            // Step 1.2 - Create script for drop database constraints - 
            scriptName = Path.Combine(AssemblyDirectory, @"DatabaseScripts\01. DB List constraints drop.sql");
            if (!File.Exists(scriptName))
                throw new FileNotFoundException(string.Format("Not found {0}", scriptName));


            outFile = Path.Combine(FolderSchema, @"log\DTL-ListConstraintsDrop.log");
            arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --file ""{3}"" --output {5}",
                dbTest.Server, dbTest.Port, dbTest.Username, scriptName, dbTest.Database, outFile);
            // The result of this run is the file DTL-RemoveConstraints.inp
            result = ExecPsqlCommand(arguments);



            // Step 1.3 - Remove database constraints
            scriptName = Path.Combine(FolderSchema, @"log\DTL-RemoveConstraints.inp");
            if (!File.Exists(scriptName))
                throw new FileNotFoundException(string.Format("Not found {0}", scriptName));

            arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --file ""{3}""",
                dbTest.Server, dbTest.Port, dbTest.Username, scriptName, dbTest.Database);
            result = ExecPsqlCommand(arguments);

        }

        private void RunScriptsFillData()
        {

            // insert here your initial data

            //string scriptName, arguments;
            //bool result;

            //------------------------
            //2. FILL-DATA section
            //------------------------

            //// Step 2.1
            //scriptName = Path.Combine(AssemblyDirectory, @"DatabaseScripts\02. DB Fill data except geometries.sql");
            //if (!File.Exists(scriptName))
            //    throw new FileNotFoundException(string.Format("Not found {0}", scriptName));

            //arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --file ""{3}"" ",
            //    dbTest.Server, dbTest.Port, dbTest.Username, scriptName, dbTest.Database);
            //result = ExecPsqlCommand(arguments);

            //// Step 2.2
            //scriptName = Path.Combine(AssemblyDirectory, @"DatabaseScripts\03. DB Insert initial data.sql");
            //if (!File.Exists(scriptName))
            //    throw new FileNotFoundException(string.Format("Not found {0}", scriptName));

            //arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --file ""{3}"" ",
            //    dbTest.Server, dbTest.Port, dbTest.Username, scriptName, dbTest.Database);
            //result = ExecPsqlCommand(arguments);


        }

        public void RunScriptsPostData()
        {

            // ------------------------
            // 3. POST-DATA section (result redirect to output file)
            // ------------------------
            string scriptName, arguments;
            bool result;

            // Step 3.1 - Add database constraints
            scriptName = Path.Combine(FolderSchema, @"log\DTL-AddConstraints.inp");
            if (!File.Exists(scriptName))
                throw new FileNotFoundException(string.Format("Not found {0}", scriptName));

            arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --file ""{3}""",
                dbTest.Server, dbTest.Port, dbTest.Username, scriptName, dbTest.Database);
            result = ExecPsqlCommand(arguments);

        }

        private bool ExecPsqlCommand(string psqlArguments)
        {

            try
            {
                // use psql command

                ProcessStartInfo processInfo = CreateProcessInfo(psqlExe, psqlArguments);

                string errorFile = Path.Combine(FolderSchema, @"log\DTL-ExecScripts.log");

                Debug.WriteLine("\r\n" + psqlExe + psqlArguments);

                Debug.Write(string.Format("INFO: Running script ..."));

                RunProcess(processInfo, errorFile);

                Debug.WriteLine("   OK!");

                return true;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

        }

        private string GetMachineNameFromIPAddress(string ipAdress)
        {
            string machineName = string.Empty;
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ipAdress);
                machineName = hostEntry.HostName.ToUpper();
            }
            catch (Exception)
            {
                // null action
            }
            return machineName;
        }

        private string GetServicePath(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController svc in services)
            {
                if (svc.ServiceName.Contains(serviceName))
                {
                    Console.WriteLine();
                    Console.WriteLine("  Service :        {0}", svc.ServiceName);
                    Console.WriteLine("    Display name:    {0}", svc.DisplayName);

                    ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + svc.ServiceName + "'");

                    wmiService.Get();

                    Console.WriteLine("    Start name:      {0}", wmiService["StartName"]);
                    Console.WriteLine("    Description:     {0}", wmiService["Description"]);
                    Console.WriteLine("    PathName   :     {0}", wmiService["PathName"]);

                    string pathName = wmiService["PathName"].ToString();
                    int end = pathName.IndexOf(".exe");
                    string serviceExecutable = pathName.Substring(1, end + 3);
                    string pathExecutable = Path.GetDirectoryName(serviceExecutable);

                    Console.WriteLine("    PathExe    :     {0}", pathExecutable);

                    return pathExecutable;
                }
            }

            return null;
        }

    }
}
