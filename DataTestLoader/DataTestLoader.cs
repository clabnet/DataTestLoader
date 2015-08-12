using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.Configuration;
using System.ServiceProcess;
using System.Management;
using System.Data;
using System.Transactions;

using Newtonsoft.Json;

using Dapper;

using ServiceStack.Text;


namespace DataTestLoader
{
    public class DataTestLoader : BaseClass
    {

        public DataTestLoader(bool refreshSchema = false, bool initDatabase = false, bool loadJsonData = false)
        {
            bool weCanProceed = true;

            DatabaseScriptManager dMan = new DatabaseScriptManager(refreshSchema, initDatabase, loadJsonData);

            if (refreshSchema)
                weCanProceed = dMan.RefreshDatabaseSchema();

            if (initDatabase && weCanProceed)
                dMan.InitDatabase();

            if (loadJsonData)
                this.RunDataTestLoader();

            if (initDatabase)
                dMan.RunScriptsPostData();

            Debug.WriteLine(string.Format("\r\nINFO: DataTestLoader execution finished.", this.TotalRecordsAdded));
        }

        private void RunDataTestLoader()
        {
            this.TotalRecordsAdded = 0;

            List<string> tablesList = RetrieveTablesList();
            if (tablesList.Count == 0)
                throw new ApplicationException("Please add at least one table name into TablesToLoad.json.");

            foreach (string tableName in tablesList)
                this.TotalRecordsAdded += AddRows(tableName);

            Debug.WriteLine(string.Format("\r\nINFO: Total records added : {0}", this.TotalRecordsAdded));
        }

        protected internal object currentRecord;

        protected internal List<string> RetrieveTablesList()
        {
            string fileName = @"DataTestFiles\TablesToLoad.json";
            string textFile = ReadFileContent(fileName);
            return JsonConvert.DeserializeObject<List<string>>(textFile);
        }

        protected internal bool CheckFilesOnDisk()
        {
            List<string> tablesToLoad = this.RetrieveTablesList();

            foreach (var tableName in tablesToLoad)
            {
                string fullFileName = Path.Combine(AssemblyDirectory, "DataTestFiles", tableName + ".json");

                if (!File.Exists(fullFileName))
                    throw new ApplicationException(string.Format("File {0} was not found. Load the file on disk or remove it from list of tables to load.", fullFileName));
            }

            return true;
        }

        protected internal int AddRows(string tableName)
        {
            try
            {

                int cntRead = 0;
                int cntInsert = 0;

                string fileName = @"DataTestFiles\" + tableName + ".json";
                string json = ReadFileContent(fileName);

                // we use Type.GetType(string) to get the Type object associated with a type by its name
                string fullQualifiedClassName = string.Format("{0}.{1}, {2}", AssemblyModelNamespace, tableName, AssemblyModel);

                Type myType = Type.GetType(fullQualifiedClassName);
                if (myType == null)
                    throw new ApplicationException(string.Format("The type '{0}' or assembly '{1}' or namespace '{2}' was not found on {3}", tableName, AssemblyModel, AssemblyModelNamespace, AssemblyDirectory));

                var recs = DeserializeList(json, myType);

                using (var cnn = ConnectionFactory.GetOpenConnection())
                {

                    IDbTransaction transaction = cnn.BeginTransaction();

                    try
                    {
                        foreach (var rec in recs)
                        {
                            this.currentRecord = rec;

                            // Debug.WriteLine(rec.Dump());

                            // using patches of YogirajA and Clabnet
                            // https://github.com/YogirajA/Dapper.SimpleCRUD/blob/feature/AssociativeInsert/Dapper.SimpleCRUD/SimpleCRUD.cs
                            cnn.InsertNoPkConstraint(rec);

                            // using the last version of Eric's code modified int as long
                            // cnn.InsertAsync(rec, transaction);

                            cntRead++;
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine(string.Format("\r\nERROR: Rollback insert on table {0} !", tableName));
                        Debug.WriteLine(ex.Message);
                        throw;
                    }

                    string sqlCount = string.Format("SELECT COUNT(*) AS count FROM {0}", tableName);

                    var recCounter = cnn.ExecuteScalar(sqlCount);
                    cntInsert = Convert.ToInt32(recCounter);

                    if (cntRead != cntInsert)
                        throw new ApplicationException(string.Format("\r\nERROR : Error adding data on {0} table. Read {1}, added {2} records.", tableName, cntRead, cntInsert));

                }

                Debug.WriteLine(string.Format("\r\nINFO: Added {0} records on table {1}.   OK!", cntInsert, tableName));

                return cntRead;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("\r\nERROR : This is the current record when occurred error on {0} table :", tableName));
                Debug.WriteLine(this.currentRecord.Dump());
                Debug.WriteLine(ex.Message);
                throw;
            }

        }

        protected internal IList DeserializeList(string value, Type type)
        {
            var listType = typeof(List<>).MakeGenericType(type);
            var list = JsonConvert.DeserializeObject(value, listType);
            return list as IList;
        }

        protected internal int TotalRecordsAdded { get; set; }
    }
}
