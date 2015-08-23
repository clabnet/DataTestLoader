using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DataTestLoader
{

    public abstract class BaseClass
    {
        protected internal bool refreshSchema;
        protected internal bool initDatabase;
        protected internal bool loadJsonData;

        protected internal string FileSchemaPreData { get; set; }
        protected internal string FileSchemaPostData { get; set; }
        protected internal string FileSchemaFullName { get; set; }

        protected internal string FolderSchema
        {
            get
            {
                return Path.Combine(AssemblyDirectory, "DatabaseScripts");
            }
        }

        public string ConnectionStringDBSource
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DBSource"].ConnectionString;
            }
        }
        public string ConnectionStringDBTest
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DBTest"].ConnectionString;
            }
        }
        protected internal static string ConnectionStringDBPostgres
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DBPostgres"].ConnectionString;
            }
        }

        protected internal static bool ConfigKeyExist(string name)
        {
            // Return false if it doesn't exist, true if it does
            return ConfigurationManager.AppSettings[name] != null;
        }

        protected internal static bool ConnectionExist(string name)
        {
            // Return false if it doesn't exist, true if it does
            return ConfigurationManager.ConnectionStrings[name] != null;
        }
                
        protected internal static string AssemblyModel
        {
            get
            {
                return ConfigurationManager.AppSettings["AssemblyModel"].ToString();
            }
        }
        protected internal static string AssemblyModelNamespace
        {
            get
            {
                return ConfigurationManager.AppSettings["AssemblyModelNamespace"].ToString();
            }
        }
        protected internal static string AssemblyDirectory
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }
        protected internal static string CurrentProjectFolder
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory.CurrentProjectFolder();
            }
        }

        protected internal static string ProjectBaseFolder
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory.GetProjectBaseFolder();
            }
        }

        protected internal string ReadFileContent(string fileName)
        {
          
            if (fileName == string.Empty)
                throw new DataTestLoaderException(string.Format("Missing file {0}.", fileName));

            string fullFileName = Path.Combine(AssemblyDirectory, fileName);

            if (!File.Exists(fullFileName))
                throw new DataTestLoaderException(string.Format("File {0} or path not found.", fullFileName));

            try
            {
                using (StreamReader sr = new StreamReader(fullFileName))
                    return sr.ReadToEnd();
            }
            catch (IOException ex)
            {
                throw new DataTestLoaderException(ex);
            }
        }

    }
}
