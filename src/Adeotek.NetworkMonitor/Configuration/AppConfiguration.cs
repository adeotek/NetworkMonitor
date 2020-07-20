using System;
using System.Collections.Generic;

namespace Adeotek.NetworkMonitor.Configuration
{
    public class AppConfiguration
    {
        public static readonly string[] WriteDataToTypes = {"LOCALCSVFILE", "POSTGRESQL", "GOOGLESPREADSHEETS"};
        
        public List<TestConfiguration> Tests { get; set; }
        public List<DataTarget> WriteDataTo { get; set; }
        public string AppPath { get; set; }

        public static string GetConnectionString(Dictionary<string, string> config)
        {
            var dbServer = config.ContainsKey("Server") ? config["Server"] : null;
            var dbUser = config.ContainsKey("Server") ? config["User"] : null;
            var dbName = config.ContainsKey("Server") ? config["Database"] : null;
            if (string.IsNullOrEmpty(dbServer) || string.IsNullOrEmpty(dbUser) || string.IsNullOrEmpty(dbName))
            {
                throw new Exception("Invalid database configuration!");
            }

            var dbPort = string.IsNullOrEmpty(config.ContainsKey("Port") ? config["Port"] : null)
                ? string.Empty
                : $"Port={config["Port"]};";
            var dbPassword = config.ContainsKey("Server") ? config["Password"] : null;
            return $"Host={dbServer};{dbPort}User ID={dbUser};Password={dbPassword};Database={dbName}";
        }
    }
}