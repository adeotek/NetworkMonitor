using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Adeotek.NetworkMonitor.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Adeotek.NetworkMonitor
{
    public class NetworkMonitor
    {
        private static readonly string[] WriteDataToTypes = {"LOCALCSVFILE", "POSTGRESQL", "GOOGLESPREADSHEETS"};  
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public NetworkMonitor(IConfiguration configuration, ILogger logger, string appPath)
        {
            _logger = logger;
            _appConfiguration = configuration.GetSection("Application").Get<AppConfiguration>();
            if (_appConfiguration == null)
            {
                throw new Exception("Invalid or missing [Application] configuration section!");
            }

            _appConfiguration.AppPath = appPath;
            _logger?.LogInformation("NetworkMonitor initialized.");
        }

        public void RunAllTests()
        {
            _logger?.LogDebug("Starting all tests...");
            var timer = new Stopwatch();
            timer.Start();
            
            if (_appConfiguration?.PingTest.Enabled ?? false)
            {

                RunPingTest();
            }

            if (_appConfiguration?.SpeedTest.Enabled ?? false)
            {
                RunSpeedTest();
            }

            timer.Stop();
            _logger?.LogDebug($"All tests done in {timer.ElapsedMilliseconds/1000:#0.000} sec.");
        }

        public void RunPingTest()
        {
            _logger?.LogDebug("Starting ping test...");
            if ((_appConfiguration?.PingTest?.Targets?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No ping targets found!");
                return;
            }
            
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var pinger = new Pinger();
                var results = _appConfiguration.PingTest.Targets.Select(target => pinger.SafePing(target)).ToList();
                var resultsWrite = WritePingTestResults(results, _appConfiguration.PingTest?.WriteToCollection);
                if(!resultsWrite)
                {
                    _logger?.LogInformation($"Ping test results: {JsonSerializer.Serialize(results)}");
                }
                timer.Stop();
                _logger?.LogInformation($"Ping test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running ping test!");
            }
        }

        public void RunSpeedTest()
        {
            _logger?.LogDebug("Starting speed test...");
            if ((_appConfiguration?.SpeedTest?.Servers?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No speed test servers found!");
                return;
            }

            try
            {
                var timer = new Stopwatch();
                timer.Start();
                


                timer.Stop();
                _logger?.LogDebug($"Speed test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running speed test!");
            }
        }

        private bool WritePingTestResults(IList<PingResult> results, string writeToCollection)
        {
            if ((_appConfiguration.WriteDataTo?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No WriteDataTo items present in configuration!");
                return false;
            }

            var result = false;
            foreach (var target in _appConfiguration.WriteDataTo)
            {
                try
                {
                    if (target == null || string.IsNullOrEmpty(target?.Type) ||
                        !WriteDataToTypes.Contains(target.Type.ToUpper()))
                    {
                        throw new Exception($"Invalid WriteDataTo item configuration: [{target?.Type ?? string.Empty}]");
                    }

                    switch (target.Type.ToUpper())
                    {
                        case "LOCALCSVFILE":
                            WriteResultsToCsv(results, writeToCollection, target.Configuration);
                            break;
                        case "POSTGRESQL":
                            WriteResultsToSql(results, writeToCollection, target.Configuration);
                            break;
                        case "GOOGLESPREADSHEETS":
                            WriteResultsToGoogleSpreadsheets(results, writeToCollection, target.Configuration);
                            break;
                        default:
                            continue;
                    }
                        
                    result = true;
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Unable to write ping test results!");
                }
            }

            return result;
        }

        private void WriteResultsToCsv(ICollection<PingResult> data, string fileName, Dictionary<string, string> config)
        {
            if ((data?.Count ?? 0) == 0)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(fileName))
            {
                throw new Exception("Null or empty CSV file name!");
            }
            
            var path = config.ContainsKey("Path") ? config["Path"] : null;
            string csvFile;
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            {
                csvFile = Path.Join(_appConfiguration.AppPath, fileName + ".csv");
            }
            else
            {
                csvFile =  Path.Join(_appConfiguration.AppPath, path, fileName + ".csv");
            }

            var writeHeaderData = !File.Exists(csvFile);
            using var fileWriter = new StreamWriter(new FileStream(csvFile, FileMode.OpenOrCreate, FileAccess.Write));
            if (writeHeaderData)
            {
                fileWriter.WriteLine("\"Timestamp\",\"Target\",\"Duration\",\"Message\"");
            }

            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }
                fileWriter.WriteLine($"\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",\"{item.Target}\",{item?.Time.ToString()},\"{item.Message}\"");
            }
        }
        
        private void WriteResultsToSql(ICollection<PingResult> data, string tableName, Dictionary<string, string> config)
        {
            if ((data?.Count ?? 0) == 0)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(tableName))
            {
                throw new Exception("Invalid table name!");
            }

            var dbSchema = config.ContainsKey("Schema") ? config["Schema"] : null;
            if (string.IsNullOrEmpty(dbSchema))
            {
                throw new Exception("Invalid database schema!");
            }

            var connectionString = GetConnectionString(config);
            using var dbConnection = new NpgsqlConnection(connectionString);
            try
            {
                dbConnection.Open();
            }
            catch (Exception e)
            {
                _logger?.LogError($"Invalid database connection string: [{connectionString}]");
                throw;
            }
            var queryString = $"insert into \"{dbSchema}\".\"{tableName}\" (\"timestamp\",\"target\",\"duration\",\"message\") values ";
            var first = true;
            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }

                if (first)
                {
                    first = false;
                }
                else
                {
                    queryString += ", ";
                }
                queryString += $"('{DateTime.Now:yyyy-MM-dd HH:mm:ss}','{item.Target}',{(item.Success ? item.Time.ToString() : "null")},'{item.Message ?? string.Empty}')";
            }
            queryString += ";";
            using var command = new NpgsqlCommand(queryString, dbConnection);
            command.Prepare();
            command.ExecuteNonQuery();
        }
        
        private void WriteResultsToGoogleSpreadsheets(ICollection<PingResult> data, string sheetName, Dictionary<string, string> config)
        {
            if ((data?.Count ?? 0) == 0)
            {
                return;
            }
            
            var gSheets = new GSheets(config, _appConfiguration.AppPath);
            if (!gSheets.CreateSheetIfMissing(sheetName))
            {
                throw new Exception($"Unable to create Google spreadsheet sheet [{sheetName}]!");
            }
        
            if ((data?.Count ?? 0) == 0)
            {
                return;
            }
        
            var rNoData = gSheets.ReadRange("A1:A1", sheetName);
            if ((rNoData?.Count ?? 0) == 0 || (rNoData[0]?.Count ?? 0) == 0 || string.IsNullOrEmpty(rNoData[0][0].ToString()))
            {
                throw new Exception("Unable to get row number cell data!");
            }
        
            if (!int.TryParse(rNoData[0][0].ToString()?.Substring(4), out var rNo))
            {
                throw new Exception("Unable to parse row number cell data!");
            }
        
            var listItem = new List<object> { rNo + 1, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            var range = $"A{rNo + 2}:{GSheets.GetNextLetter('B', data.Count * 2)}{rNo + 2}";
            foreach (var item in data)
            {
                if (item?.Success ?? false)
                {
                    listItem.Add(item.Time);
                }
                else
                {
                    listItem.Add(null);
                }
            }
            listItem.AddRange(data.Select(item => item?.Success ?? false ? null : item?.Message ?? "N/A"));
            var values = new List<IList<object>> { listItem };
        
            gSheets.WriteRange(values, range, sheetName);
        }

        private static string GetConnectionString(Dictionary<string, string> config)
        {
            var dbServer = config.ContainsKey("Server") ? config["Server"] : null;
            var dbUser = config.ContainsKey("Server") ? config["User"] : null;
            var dbName = config.ContainsKey("Server") ? config["Database"] : null;
            if (string.IsNullOrEmpty(dbServer) || string.IsNullOrEmpty(dbUser) || string.IsNullOrEmpty(dbName))
            {
                throw new Exception("Invalid database configuration!");
            }

            var dbPort = string.IsNullOrEmpty(config.ContainsKey("Port") ? config["Port"] : null) ? string.Empty : $"Port={config["Port"]};";
            var dbPassword = config.ContainsKey("Server") ? config["Password"] : null;
            // if (((config.ContainsKey("EncryptPassword") ? config["EncryptPassword"] : null) ?? string.Empty).ToLower() == "true")
            // {
            //     dbPassword = MD5()
            // }

            return $"Host={dbServer};{dbPort}User ID={dbUser};Password={dbPassword};Database={dbName}";
        }
    }
}
