using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Adeotek.NetworkMonitor.Configuration;
using Adeotek.NetworkMonitor.Results;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Adeotek.NetworkMonitor.Writers
{
    public class PostgreSqlWriter<T> : IResultWriter
    {
        private readonly Dictionary<string, string> _config;
        private readonly ILogger _logger;
        private readonly string _appPath;
        
        public PostgreSqlWriter(Dictionary<string, string> config, ILogger logger, string appPath)
        {
            _config = config;
            _logger = logger;
            _appPath = appPath;
        }
        
        public void WriteResults(ICollection<ITestResult> data, string collection, string group)
        {
            if ((data?.Count ?? 0) == 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(collection))
            {
                throw new Exception("Invalid table name!");
            }

            var dbSchema = _config.ContainsKey("Schema") ? _config["Schema"] : null;
            if (string.IsNullOrEmpty(dbSchema))
            {
                throw new Exception("Invalid database schema!");
            }

            var useFileBuffer = ((_config.ContainsKey("UseFileBuffer") ? _config["UseFileBuffer"] : null) ?? string.Empty).ToLower() == "true";
            var connectionString = AppConfiguration.GetConnectionString(_config);
            using var dbConnection = new NpgsqlConnection(connectionString);
            try
            {
                dbConnection.Open();
            }
            catch (Exception)
            {
                _logger?.LogError($"Invalid database connection string: [{connectionString}]");
                if (useFileBuffer)
                {
                    WriteResultsToBufferFile(data, collection, "sql");
                }

                throw;
            }

            try
            {
                if(!CheckIfTableExists(collection, dbSchema, connectionString))
                {
                    var dbUser = _config.ContainsKey("User") ? _config["User"] : null;
                    CreateTable(data.First().GetFieldsForTableCreate(), collection, dbSchema, connectionString, dbUser);
                }
            }
            catch (Exception)
            {
                _logger?.LogError($"Unable to create mission database table: [{dbSchema}.{collection}]");
                if (useFileBuffer)
                {
                    WriteResultsToBufferFile(data, collection, "sql");
                }

                throw;
            }

            var bufferedData = ReadResultsFromBufferFile(collection, "sql");
            if ((bufferedData?.Count ?? 0) > 0)
            {
                foreach (var item in bufferedData)
                {
                    data.Add(item);
                }
            }
            
            var queryString = $"insert into \"{dbSchema}\".\"{collection}\" (\"{string.Join("\",\"", data.First().GetFields())}\") values ";
            var first = true;
            foreach (var item in data.Where(item => item != null))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    queryString += ", ";
                }

                queryString += item.ToSqlInsertString();
            }

            queryString += ";";
            using var command = new NpgsqlCommand(queryString, dbConnection);
            command.Prepare();
            command.ExecuteNonQuery();
        }
        
        private bool CheckIfTableExists(string collectionName, string schema, string connectionString)
        {
            using var dbConnection = new NpgsqlConnection(connectionString);
            dbConnection.Open();
            var queryString = $"select count(1) from pg_catalog.pg_class c inner join pg_catalog.pg_namespace n on n.oid = c.relnamespace where c.relkind= 'r' and n.nspname = '{schema}' and c.relname = '{collectionName}'";
            using var command = new NpgsqlCommand(queryString, dbConnection);
            command.Prepare();
            command.ExecuteNonQuery();
            var result = command.ExecuteScalar();
            return result.ToString() == "1";
        }

        private void CreateTable(List<string> fields, string collectionName, string schema, string connectionString, string dbUser = null)
        {
            var fieldsString = string.Join(", ", fields);
            if (string.IsNullOrEmpty(fieldsString))
            {
                throw new Exception("Null or empty fields list!");
            }
                
            using var dbConnection = new NpgsqlConnection(connectionString);
            dbConnection.Open();
            
            var queryString = $"create table \"{schema}\".\"{collectionName}\" ({fieldsString});";
            using (var command1 = new NpgsqlCommand(queryString, dbConnection))
            {
                command1.ExecuteNonQuery();
            }

            if (string.IsNullOrEmpty(dbUser))
            {
                return;
            }
            queryString = $"alter table \"{schema}\".\"{collectionName}\" owner to \"{dbUser}\";";
            using (var command2 = new NpgsqlCommand(queryString, dbConnection))
            {
                command2.ExecuteNonQuery();
            }
        }
        
        private void WriteResultsToBufferFile(IEnumerable<ITestResult> data, string collectionName, string sourceName)
        {
            if (!Directory.Exists(Path.Join(_appPath, "data", sourceName)))
            {
                Directory.CreateDirectory(Path.Join(_appPath, "data", sourceName));
            }

            var fileName = Path.Join(_appPath, "data", sourceName, collectionName + ".temp");
            using var fileWriter = File.AppendText(fileName);
            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }

                fileWriter.WriteLine(item.ToJson());
            }
        }

        private ICollection<ITestResult> ReadResultsFromBufferFile(string collectionName, string sourceName)
        {
            var fileName = Path.Join(_appPath, "data", sourceName, collectionName + ".temp");
            if (!File.Exists(fileName))
            {
                return null;
            }

            var data = new List<ITestResult>();
            try
            {
                using (var fileReader = File.OpenText(fileName))
                {
                    string line;
                    while ((line = fileReader.ReadLine()) != null)
                    {
                        var item = JsonSerializer.Deserialize<T>(line) as ITestResult;
                        if (item != null)
                        {
                            data.Add(item);
                        }
                    }
                }

                File.Delete(fileName);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Unable to read buffered data from file: [{fileName}]");
                data = null;
            }

            return data;
        }
    }
}