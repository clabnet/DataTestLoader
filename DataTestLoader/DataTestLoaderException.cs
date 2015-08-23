using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTestLoader
{
    [Serializable]
    public class DataTestLoaderException : Exception
    {

        #region NLog Logger class definition

        /// <summary>
        /// Log class definition
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        public DataTestLoaderException(string message)
            : base(message)
        {
            
            logger.Error(message);
            logger.Warn("DataTestLoader ended abnormally.");

            System.Environment.Exit(-1);
        }

        public DataTestLoaderException(string message, Exception inner)
            : base(message, inner) {

            logger.Fatal(inner, message);
            logger.Warn("DataTestLoader ended abnormally.");

            System.Environment.Exit(-1);
        }

        public DataTestLoaderException(Exception inner)
            : base(inner.Message)
        {
            
            logger.Fatal(inner);
            logger.Warn("DataTestLoader ended abnormally.");

            System.Environment.Exit(-1);
        }

    }
}
