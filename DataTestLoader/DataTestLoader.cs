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
using NLog;

using Dapper;

using ServiceStack.Text;

namespace DataTestLoader
{
    public class DataTestLoader : BaseClass
    {

        #region NLog Logger class definition

        /// <summary>
        /// Log class definition
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        public DataTestLoader(bool refreshSchema = false, bool initDatabase = false, bool loadJsonData = false, string testSuite = "")
        {
            this.refreshSchema = refreshSchema;
            this.initDatabase = initDatabase;
            this.loadJsonData = loadJsonData;
            this.testSuite = testSuite;

            PostgresqlScriptManager dMan = new PostgresqlScriptManager(refreshSchema, initDatabase, loadJsonData, testSuite);

            bool weCanProceed = dMan.RefreshDatabaseSchema();

            if (initDatabase && weCanProceed)
                dMan.InitDatabase();

            if (loadJsonData)
                this.RunDataTestLoader();

            if (initDatabase)
                dMan.ApplySchemaPostData();

            logger.Info("DataTestLoader execution completed.");
        }

        private void RunDataTestLoader()
        {
            this.TotalRecordsAdded = 0;

            List<string> tablesList = RetrieveTablesList();
            if (tablesList.Count == 0)
            {
                string err = "Please add at least one table name into TablesToLoad.json.";
                throw new DataTestLoaderException(err);
            }

            logger.Info(string.Format("Adding .json data from {0} folder", (string.IsNullOrEmpty(testSuite)) ? "DataTestFiles" : @"DataTestFiles\" + testSuite));

            foreach (string tableName in tablesList)
                this.TotalRecordsAdded += AddRows(tableName);

            logger.Info(string.Format("Total records added : {0}", this.TotalRecordsAdded));
        }

        protected internal object currentRecord;

        protected internal List<string> RetrieveTablesList()
        {
            string fileName = string.Format(@"DataTestFiles\{0}\TablesToLoad.json", this.testSuite);
            string textFile = ReadFileContent(fileName);
            return JsonConvert.DeserializeObject<List<string>>(textFile);
        }

        protected internal bool CheckFilesOnDisk()
        {
            List<string> tablesToLoad = this.RetrieveTablesList();

            foreach (var tableName in tablesToLoad)
            {
                string fullFileName = Path.Combine(AssemblyDirectory, string.Format(@"DataTestFiles\{0}\{1}.json", this.testSuite, tableName));

                if (!File.Exists(fullFileName))
                {
                    string err = string.Format("File {0} was not found. Add file on disk or remove it from TablesToLoad.json file.", fullFileName);
                    throw new DataTestLoaderException(err);
                }
            }

            return true;
        }

        protected internal int AddRows(string tableName)
        {
            try
            {

                int cntRead = 0;
                int cntInsert = 0;

                string fileName = string.Format(@"DataTestFiles\{0}\{1}.json", this.testSuite, tableName);
                string json = ReadFileContent(fileName);

                // we use Type.GetType(string) to get the Type object associated with a type by its name
                string fullQualifiedClassName = string.Format("{0}.{1}, {2}", AssemblyModelNamespace, tableName, AssemblyModel);

                Type myType = Type.GetType(fullQualifiedClassName);
                if (myType == null)
                {
                    string err = string.Format("The type '{0}' or assembly '{1}' or namespace '{2}' was not found on {3}", tableName, AssemblyModel, AssemblyModelNamespace, AssemblyDirectory);
                    throw new DataTestLoaderException(err);
                }

                var recs = DeserializeList(json, myType);

                using (var cnn = ConnectionFactory.GetOpenConnection())
                {

                    IDbTransaction transaction = cnn.BeginTransaction();

                    try
                    {
                        foreach (var rec in recs)
                        {
                            this.currentRecord = rec;

                            // logger.Info(rec.Dump());

                            // using patches of YogirajA and Clabnet
                            // https://github.com/YogirajA/Dapper.SimpleCRUD/blob/feature/AssociativeInsert/Dapper.SimpleCRUD/SimpleCRUD.cs
                            // cnn.InsertNoPkConstraint(rec);

                            // using the last version of Eric's code modified int as long
                            // cnn.InsertAsync(rec, transaction);

                            //  var id = connection.Insert<long>(new BigCar { Make = "Big", Model = "Car" });

                           cnn.Insert<long>(rec);

                            cntRead++;
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        string err = string.Format("Rollback insert on table {0} ! ({1})", tableName, ex.Message);
                        throw new DataTestLoaderException(err);
                    }

                    string sqlCount = string.Format("SELECT COUNT(*) AS count FROM {0}", tableName);

                    var recCounter = cnn.ExecuteScalar(sqlCount);
                    cntInsert = Convert.ToInt32(recCounter);

                    if (cntRead != cntInsert)
                    {
                        string err = string.Format("Error adding data on '{0}' table. Read {1}, added {2} records.", tableName, cntRead, cntInsert);
                        throw new DataTestLoaderException(err);
                    }
                }

                logger.Info(string.Format("Added {0} records on table '{1}'.", cntInsert, tableName));

                return cntRead;
            }
            catch (Exception ex)
            {
                string err = string.Format("This is the current record when occurred error on {0} table :", tableName);
                logger.Warn(err);
                logger.Warn(this.currentRecord.Dump());
                throw new DataTestLoaderException(err, ex);
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
