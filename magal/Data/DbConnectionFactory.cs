using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace magal.Data
{
    public static class DbConnectionFactory
    {
        // A connection string é lida do disco e parseada uma única vez por execução do app,
        // em vez de a cada chamada (que acontece a cada operação de banco no sistema todo).
        private static readonly string _connectionString = CarregarConnectionString();

        private static string CarregarConnectionString()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();

            return configuration.GetConnectionString("DefaultConnection");
        }

        public static IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}