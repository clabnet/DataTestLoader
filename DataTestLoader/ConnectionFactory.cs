using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using Dapper;

namespace DataTestLoader
{
    public static class ConnectionFactory
    {
        private const string PROVIDERNAME = "Npgsql Local";

        private static string ConnectionStringDBTest
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["DBTest"].ConnectionString;
            }
        }
        
        public static IDbConnection GetOpenConnection()
        {
            DbProviderFactory factory;
            IDbConnection connection;

            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);

            factory = DbProviderFactories.GetFactory(PROVIDERNAME);
     
            connection = factory.CreateConnection();
            connection.ConnectionString = ConnectionStringDBTest;
            connection.Open();

            return connection;
        }
    }
}
