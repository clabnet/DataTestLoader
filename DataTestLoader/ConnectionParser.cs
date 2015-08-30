using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTestLoader
{
    public class ConnectionParser
    {
        private const string PROVIDERNAME = "Npgsql";

        public string ConnectionString { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public ConnectionParser(string connectionString, string provider = null)
        {
            this.ConnectionString = connectionString;

            if (provider == null) provider = PROVIDERNAME;

            DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(provider);
            DbConnectionStringBuilder builder = dbProviderFactory.CreateConnectionStringBuilder();

            builder.ConnectionString = connectionString;

            Server = builder["host"].ToString();
            Database = builder["database"].ToString();
            Username = builder["username"].ToString();
            Password = builder["password"].ToString();

            Port = Convert.ToInt32(builder["port"]);
        }
    }

}
