using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

using Dapper;

using ServiceStack.Text;

using System.Collections;
using System.Diagnostics;
using System.Configuration;
using System.ServiceProcess;
using System.Management;
using System.Data;

namespace DataTestLoader
{
    public class DataTestLoader : BaseClass
    {

        public DataTestLoader(bool refreshSchema = false, bool initDatabase = false, bool loadJsonData = true)
        {
            DatabaseScriptManager dMan = new DatabaseScriptManager(refreshSchema, initDatabase, loadJsonData);

            if (initDatabase)
            {
                bool weCanProceed = dMan.RefreshDatabaseSchema();
                if (weCanProceed)
                    dMan.InitDatabase();
            }

            if (loadJsonData)
                this.RunDataTestLoader();

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

                int addedRecords = 0;

                string fileName = @"DataTestFiles\" + tableName + ".json";
                string json = ReadFileContent(fileName);

                // we use Type.GetType(string) to get the Type object associated with a type by its name
                string fullQualifiedClassName = string.Format("{0}.{1}, {2}", AssemblyModelNamespace, tableName, AssemblyModel);

                Type myType = Type.GetType(fullQualifiedClassName);
                if (myType == null)
                    throw new ApplicationException(string.Format("Type {0} or assembly {1} or namespace '{2}' not found on {3}", tableName, AssemblyModel, AssemblyModelNamespace, AssemblyDirectory));

                var recs = DeserializeList(json, myType);

                using (var cnn = ConnectionFactory.GetOpenConnection())
                {
                    foreach (var rec in recs)
                    {
                        this.currentRecord = rec;

                        // Debug.WriteLine(rec.Dump());

                        // using patches of YogirajA and Clabnet
                        // https://github.com/YogirajA/Dapper.SimpleCRUD/blob/feature/AssociativeInsert/Dapper.SimpleCRUD/SimpleCRUD.cs
                        cnn.InsertNoPkConstraint(rec);

                        addedRecords++;
                    }
                }

                Debug.WriteLine(string.Format("\r\nINFO: Added {0} records on table {1}.   OK!", addedRecords, tableName));

                return addedRecords;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("\r\n" + string.Format("Table {0}", tableName));

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
