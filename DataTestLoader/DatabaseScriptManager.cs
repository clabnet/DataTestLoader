using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.ServiceProcess;

using NLog;

namespace DataTestLoader
{
    public class PostgresqlScriptManager : BaseClass
    {
        #region NLog Logger class definition

        /// <summary>
        /// Log class definition
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        private string servicePath;
        private string hostName;
        private string psqlExe;
        private string pgdumpExe;
        private bool fReuseSchema = false;

        private static int cntErrors;
        private ConnectionParser dbSource;
        private ConnectionParser dbTest;
        
        public PostgresqlScriptManager(bool refreshSchema, bool initDatabase, bool loadJsonData)
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
            ApplySchemaPreData();
            RunCustomScripts();
        }

        public bool RefreshDatabaseSchema()
        {
            bool weCanProceed;

            if (this.refreshSchema)
                weCanProceed = ExportSchemasFromSourceDatabase();
            else
            {
                // reuse existing schemas
                weCanProceed = CheckExistSchemaFile(this.FileSchemaPreData) && CheckExistSchemaFile(this.FileSchemaPostData);
                fReuseSchema = true;
            }

            return weCanProceed;
        }

        private bool CheckValidSettings()
        {

            string keyName;
            string err;

            string serviceName = "postgresql-x64-9.4";

            // These keys are always required
            keyName = "DBTest";
            if (!ConnectionExist(keyName))
            {
                err = "Missing connection string " + keyName + " in configuration file";
                throw new DataTestLoaderException(err);
            }

            dbTest = new ConnectionParser(ConnectionStringDBTest);
            if (dbTest == null)
            {
                err = "Database test is invalid. ";
                throw new DataTestLoaderException(err);
            }
            if (!dbTest.Database.EndsWith("_test"))
            {
                err = "The name of database test must be contain the word '_test' at end of name.";
                throw new DataTestLoaderException(err);
            }

            keyName = "DBPostgres";
            if (!ConnectionExist(keyName))
            {
                err = "Missing connection string " + keyName + " in configuration file.";
                throw new DataTestLoaderException(err);
            }

            servicePath = GetServicePath(serviceName);
            if (string.IsNullOrEmpty(servicePath))
            {
                err = string.Format("Postgres DB service {0} is not installed on server {1}. ", serviceName, dbSource.Server);
                throw new DataTestLoaderException(err);
            }

            psqlExe = Path.Combine(this.servicePath, "psql.exe");

            pgdumpExe = Path.Combine(this.servicePath, "pg_dump.exe");

            keyName = "DBSource";
            if (!ConnectionExist(keyName))
            {
                err = "Missing connection string " + keyName + " in configuration file";
                throw new DataTestLoaderException(err);
            }

            dbSource = new ConnectionParser(ConnectionStringDBSource);
            if (dbSource == null)
            {
                err = "Database source is invalid.";
                throw new DataTestLoaderException(err);
            }

            hostName = GetMachineNameFromIPAddress(dbSource.Server);
            if (string.IsNullOrEmpty(hostName))
            {
                err = string.Format("Host {0} is not reachable. ", dbSource.Server);
                throw new DataTestLoaderException(err);
            }

            System.Environment.SetEnvironmentVariable("PGPASSWORD", dbSource.Password);

            keyName = "FileSchemaPreData";
            if (!ConfigKeyExist(keyName))
            {
                err = string.Format("Missing key " + keyName + " in configuration file");
                throw new DataTestLoaderException(err);
            }

            this.FileSchemaPreData = ConfigurationManager.AppSettings[keyName].ToString();

            keyName = "FileSchemaPostData";
            if (!ConfigKeyExist(keyName))
            {
                err = string.Format("Missing key " + keyName + " in configuration file");
                throw new DataTestLoaderException(err);
            }

            this.FileSchemaPostData = ConfigurationManager.AppSettings[keyName].ToString();
            // the assembly model is required only when it is required to load data from json
            if (this.loadJsonData == true)
            {
                keyName = "AssemblyModel";
                if (!ConfigKeyExist(keyName))
                {
                    err = string.Format("Missing key " + keyName + " in configuration file");
                    throw new DataTestLoaderException(err);
                }

                keyName = "AssemblyModelNamespace";
                if (!ConfigKeyExist(keyName))
                {
                    err = string.Format("Missing key " + keyName + " in configuration file");
                    throw new DataTestLoaderException(err);
                }

                if (!File.Exists(Path.Combine(AssemblyDirectory, AssemblyModel + ".dll")))
                {
                    err = string.Format("Assembly Model {0} was not found on {1}", AssemblyModel, AssemblyDirectory);
                    throw new FileNotFoundException();
                }
            }

            logger.Info("All settings are valid. DataTestLoader will run.");

            return true;
        }

        private bool ExportSchemasFromSourceDatabase()
        {

            // http://www.postgresonline.com/special_feature.php?sf_name=postgresql90_pg_dumprestore_cheatsheet&outputformat=html
            // http://www.commandprompt.com/ppbook/x17860
            // http://www.postgresql.org/docs/9.4/static/app-pgdump.html

            CreateSchemaPreData();

            CheckExistSchemaFile(this.FileSchemaPreData);

            CreateSchemaPostData();

            CheckExistSchemaFile(this.FileSchemaPostData);

            return true;
        }

        private void CreateSchemaPreData()
        {

            this.FileSchemaPreData = string.Empty;

            string file = String.Format("{0}-{1}-PRE-DATA.sql", hostName, dbSource.Database);
            string fullPath = Path.Combine(this.FolderSchema, file);

            // STEP 1 - use pg_dump command with PRE-DATA argument
            string arguments = String.Format(@" --host {0} --port {1} --username {2} --schema-only --section=pre-data --no-owner --no-privileges --encoding UTF8 --file {3} --dbname {4}", dbSource.Server, dbSource.Port, dbSource.Username, fullPath, dbSource.Database);

            ProcessStartInfo processInfo = CreateProcessInfo(pgdumpExe, arguments);

            logger.Info(string.Format("Waiting please, retrieving PRE-DATA schema {0} from {1} may take up two minutes ", dbSource.Database, dbSource.Server));

            RunProcess(processInfo);

            this.FileSchemaPreData = fullPath;

            if (this.FileSchemaPreData.Length > 0)
                logger.Info(string.Format("Created schema {0}", this.FileSchemaPreData));
            else
            {
                string err = string.Format("Errors on creation schema {0}. ", this.FileSchemaPreData);
                throw new DataTestLoaderException(err);
            }
        }

        private void CreateSchemaPostData()
        {
            this.FileSchemaPostData = string.Empty;

            string file = String.Format("{0}-{1}-POSTDATA.sql", hostName, dbSource.Database);
            string fullPath = Path.Combine(this.FolderSchema, file);

            // STEP 2 - use pg_dump command with POST-DATA argument
            string arguments = String.Format(@" --host {0} --port {1} --username {2} --schema-only --section=post-data --no-owner --no-privileges --encoding UTF8 --file {3} --dbname {4}", dbSource.Server, dbSource.Port, dbSource.Username, fullPath, dbSource.Database);

            ProcessStartInfo processInfo = CreateProcessInfo(pgdumpExe, arguments);

            logger.Info(string.Format("Waiting please, retrieving POSTDATA schema {0} from {1} may take up two minutes ", dbSource.Database, dbSource.Server));

            RunProcess(processInfo);

            this.FileSchemaPostData = fullPath;

            if (this.FileSchemaPostData.Length > 0)
                logger.Info(string.Format("Created schema {0}", this.FileSchemaPostData));
            else
            {
                string err = string.Format("Errors on creation schema {0}. ", this.FileSchemaPostData);
                throw new DataTestLoaderException(err);
            }
        }

        private bool CheckExistSchemaFile(string fileName)
        {
            this.FileSchemaFullName = Path.Combine(this.FolderSchema, fileName);

            if (!File.Exists(this.FileSchemaFullName))
            {
                string err = string.Format("File schema {0} not found.", this.FileSchemaFullName);
                throw new DataTestLoaderException(err);
            }

            return true;
        }

        private void DropAllConnections()
        {
            // valid for v.9.2 and above (else use procpid in substitution of pid)
            string psqlCmd = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE pid <> pg_backend_pid()";

            string arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --command ""{3}"" ", dbTest.Server, dbTest.Port, dbTest.Username, psqlCmd, dbTest.Database);

            ProcessStartInfo processInfo = CreateProcessInfo(psqlExe, arguments);

            RunProcess(processInfo, false);

            logger.Info(string.Format("Dropped all connections to database {0}", dbTest.Database));
        }

        private void CreateExtensionPostgis()
        {
            string psqlCmd = @"CREATE EXTENSION postgis;";

            string arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {4} --command ""{3}"" ", dbTest.Server, dbTest.Port, dbTest.Username, psqlCmd, dbTest.Database);

            ProcessStartInfo processInfo = CreateProcessInfo(psqlExe, arguments);

            RunProcess(processInfo);

            logger.Info(string.Format("Created extension Postgis", dbTest.Database));
        }

        private void DropDatabase()
        {
            string dropdbExe = Path.Combine(this.servicePath, "dropdb.exe");

            string arguments = String.Format(@" --host {0} --port {1} --username {2} {3}", dbTest.Server, dbTest.Port, dbTest.Username, dbTest.Database);

            ProcessStartInfo processInfo = CreateProcessInfo(dropdbExe, arguments);

            RunProcess(processInfo, false);

            logger.Info(string.Format("Dropped database {0}", dbTest.Database));
        }

        private void CreateDatabase()
        {
            string createdbExe = Path.Combine(this.servicePath, "createdb.exe");

            string arguments = String.Format(@" --host {0} --port {1} --username {2} {3}",
                dbTest.Server, dbTest.Port, dbTest.Username, dbTest.Database);

            ProcessStartInfo processInfo = CreateProcessInfo(createdbExe, arguments);

            RunProcess(processInfo);

            logger.Info(string.Format("Created database {0}", dbTest.Database));
        }

        private static void RunProcess(ProcessStartInfo processInfo)
        {
            RunProcess(processInfo, true);
        }

        private static void RunProcess(ProcessStartInfo processInfo, bool emitErrors = true)
        {
            logger.Debug(processInfo.FileName + processInfo.Arguments);
        
            using (Process process = Process.Start(processInfo))
            {
                StreamReader err = process.StandardError;
                string errorMessage = err.ReadToEnd();
                if (errorMessage != string.Empty && emitErrors == true)
                {
                    logger.Warn(errorMessage);
                    cntErrors++;
                }
            }
        }

        private ProcessStartInfo CreateProcessInfo(string exeName, string arguments)
        {
            if (!File.Exists(exeName))
            {
                string err = string.Format("File not found {0}", exeName);
                throw new DataTestLoaderException(err);
            }

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

        private void RunCustomScripts()
        {
            // insert here your custom scripts to add initial data to database as is

            //string scriptName, arguments;

            //scriptName = Path.Combine(AssemblyDirectory, @"DatabaseScripts\02. DB Fill data except geometries.sql");
            //if (!File.Exists(scriptName))
            //{
            //    string err = string.Format("File not found {0}", scriptName);
            //    throw new DataTestLoaderException(err);
            //}

            // RunPsqlScript(scriptName);

            // -----------------

            //scriptName = Path.Combine(AssemblyDirectory, @"DatabaseScripts\03. DB Insert initial data.sql");
            //if (!File.Exists(scriptName))
            //{
            //    string err = string.Format("File not found {0}", scriptName);
            //    throw new DataTestLoaderException(err);
            //}

            // RunPsqlScript(scriptName);
        }

        public void ApplySchemaPreData()
        {
            if (fReuseSchema)
                logger.Info("Reusing schema on {0} folder for optimize time execution.", this.FolderSchema);

            RunPsqlScript(this.FileSchemaPreData);

            logger.Info("Apply schema {0} to database {1}", this.FileSchemaPreData, dbTest.Database);
        }

        public void ApplySchemaPostData()
        {
            RunPsqlScript(this.FileSchemaPostData);

            logger.Info("Apply schema {0} to database {1}", this.FileSchemaPostData, dbTest.Database);

            if (cntErrors > 0)
                logger.Warn(string.Format("Init database {0} completed with errors.", dbTest.Database));
            else
                logger.Info(string.Format("Init database {0} completed successfully.", dbTest.Database));
        }

        private void RunPsqlScript(string scriptName)
        {
            string fileName = Path.Combine(this.FolderSchema, scriptName);

            if (!File.Exists(fileName))
            {
                string err = string.Format("File not found {0}", fileName);
                throw new DataTestLoaderException(err); 
            }

            string arguments = String.Format(@" --host {0} --port {1} --username {2} --dbname {3} --file ""{4}""",
                dbTest.Server, dbTest.Port, dbTest.Username, dbTest.Database, fileName);

            ProcessStartInfo processInfo = CreateProcessInfo(psqlExe, arguments);

            RunProcess(processInfo);
        }

        private string GetMachineNameFromIPAddress(string ipAdress)
        {
            string machineName = string.Empty;
            
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ipAdress);
                machineName = hostEntry.HostName.ToUpper();
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message);
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
                    logger.Debug("Found database instance {0}", svc.DisplayName);

                    ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + svc.ServiceName + "'");

                    wmiService.Get();

                    string pathName = wmiService["PathName"].ToString();
                    int end = pathName.IndexOf(".exe");
                    string serviceExecutable = pathName.Substring(1, end + 3);
                    string pathExecutable = Path.GetDirectoryName(serviceExecutable);

                    logger.Debug("PathExe :{0}", pathExecutable);

                    return pathExecutable;
                }
            }

            return null;
        }

    }
}
