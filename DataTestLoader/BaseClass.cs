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

        public string FileSchemaPreData { get; set; }

        public string FileSchemaPostData { get; set; }

        protected internal string FileSchemaFullName { get; set; }

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
            TextWriterTraceListener[] listeners = new TextWriterTraceListener[] 
            {
                new TextWriterTraceListener(Console.Out) 
            };
        }

        #region Private methods

        protected static bool ConfigKeyExist(string name)
        {
            // Return false if it doesn't exist, true if it does
            return ConfigurationManager.AppSettings[name] != null;
        }

        protected static bool ConnectionExist(string name)
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
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        protected static string CurrentProjectFolder
        {
            get
            {
                return AppDomain.CurrentDomain.BaseDirectory.CurrentProjectFolder();
            }
        }

        protected string ReadFileContent(string fileName)
        {
            if (fileName == string.Empty)
                throw new ArgumentException("Missing fileName.");

            string fullFileName = Path.Combine(AssemblyDirectory, fileName);

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
