using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace magal.Data
{
    public static class DbConnectionFactory
    {
        public static IDbConnection CreateConnection()
        {
            // Busca o appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            return new MySqlConnection(connectionString);
        }
    }
}