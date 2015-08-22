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

        public DataTestLoaderException() { }

        public DataTestLoaderException(string message)
            : base(message)
        {
            logger.Error(message);
        }

        public DataTestLoaderException(string message, Exception inner)
            : base(message, inner) {
                logger.Error(inner, message);
        }

        public DataTestLoaderException(Exception inner)
            : base(inner.Message)
        {
            logger.Fatal(inner);
        }

        protected DataTestLoaderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) {
        }
    }
}
