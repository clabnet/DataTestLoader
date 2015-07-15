using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DataTestLoader
{

    public class BaseClass
    {
        protected internal bool refreshSchema;
        protected internal bool initDatabase;
        protected internal bool loadJsonData;

        private string _baseDirectory;

        public string FileSchemaName { get; set; }

        public string FileSchemaFullName { get; set; }

        public static string ConnectionStringDBSource
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DBSource"].ConnectionString;
            }
        }

        public static string ConnectionStringDBTest
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DBTest"].ConnectionString;
            }
        }

        public BaseClass()
        {
            _baseDirectory = AssemblyDirectory;

            TextWriterTraceListener[] listeners = new TextWriterTraceListener[] 
            {
                new TextWriterTraceListener(Console.Out) 
            };

            string name = "DBSource";
            if (!ConnectionExist(name))
                throw new ApplicationException("Missing connection string " + name + " in configuration file");

            name = "DBTest";
            if (!ConnectionExist(name))
                throw new ApplicationException("Missing connection string " + name + " in configuration file");

            name = "DBPostgres";
            if (!ConnectionExist(name))
                throw new ApplicationException("Missing connection string " + name + " in configuration file");

            name = "FileSchema";
            if (!ConfigKeyExist(name))
                throw new ApplicationException("Missing key " + name + " in configuration file");

            name = "FolderSchema";
            if (!ConfigKeyExist(name))
                throw new ApplicationException("Missing key " + name + " in configuration file");

            name = "AssemblyModel";
            if (!ConfigKeyExist(name))
                throw new ApplicationException("Missing key " + name + " in configuration file");

            name = "AssemblyModelNamespace";
            if (!ConfigKeyExist(name))
                throw new ApplicationException("Missing key " + name + " in configuration file");

            this.FileSchemaName = ConfigurationManager.AppSettings["FileSchema"].ToString();


        }

        #region Private methods

        private static bool ConfigKeyExist(string name)
        {
            // Return false if it doesn't exist, true if it does
            return ConfigurationManager.AppSettings[name] != null;
        }

        private static bool ConnectionExist(string name)
        {
            // Return false if it doesn't exist, true if it does
            return ConfigurationManager.ConnectionStrings[name] != null;
        }

        #endregion

        protected static string ConnectionStringDBPostgres
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DBPostgres"].ConnectionString;
            }
        }

        protected static string FolderSchema
        {
            get
            {
                return ConfigurationManager.AppSettings["FolderSchema"].ToString();
            }
        }

        protected static string AssemblyModel
        {
            get
            {
                return ConfigurationManager.AppSettings["AssemblyModel"].ToString();
            }
        }

        protected static string AssemblyModelNamespace
        {
            get
            {
                return ConfigurationManager.AppSettings["AssemblyModelNamespace"].ToString();
            }
        }

        protected static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        protected string ReadFileContent(string fileName)
        {
            if (fileName == string.Empty)
                throw new ArgumentException("Missing fileName.");

            string fullFileName = Path.Combine(_baseDirectory, fileName);

            try
            {
                using (StreamReader sr = new StreamReader(fullFileName))
                    return sr.ReadToEnd();
            }
            catch (IOException ex)
            {
                throw new ApplicationException(String.Format("Could not open file {0}", fullFileName), ex);
            }
        }

    }
}