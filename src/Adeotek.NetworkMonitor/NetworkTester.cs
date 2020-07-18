using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Adeotek.NetworkMonitor.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Adeotek.NetworkMonitor
{
    public abstract class NetworkTester
    {
        protected readonly AppConfiguration AppConfiguration;
        protected readonly ILogger Logger;

        protected NetworkTester(AppConfiguration appConfiguration, ILogger logger)
        {
            AppConfiguration = appConfiguration;
            Logger = logger;
        }

        protected virtual bool WriteTestResults(IList<ITestResult> results, string writeToCollection)
        {
            if ((AppConfiguration.WriteDataTo?.Count ?? 0) == 0)
            {
                Logger?.LogWarning("No WriteDataTo items present in configuration!");
                return false;
            }

            var result = false;
            foreach (var target in AppConfiguration.WriteDataTo)
            {
                try
                {
                    if (string.IsNullOrEmpty(target?.Type) ||
                        !((IList) AppConfiguration.WriteDataToTypes).Contains(target.Type.ToUpper()))
                    {
                        throw new Exception(
                            $"Invalid WriteDataTo item configuration: [{target?.Type ?? string.Empty}]");
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
                    Logger?.LogError(e, "Unable to write ping test results!");
                }
            }

            return result;
        }

        private void WriteResultsToCsv(ICollection<ITestResult> data, string fileName,
            IReadOnlyDictionary<string, string> config)
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
                csvFile = Path.Join(AppConfiguration.AppPath, fileName + ".csv");
            }
            else
            {
                if (!Directory.Exists(Path.Join(AppConfiguration.AppPath, path)))
                {
                    Directory.CreateDirectory(Path.Join(AppConfiguration.AppPath, path));
                }

                csvFile = Path.Join(AppConfiguration.AppPath, path, fileName + ".csv");
            }

            var writeHeaderData = !File.Exists(csvFile);
            using var fileWriter = new StreamWriter(new FileStream(csvFile, FileMode.OpenOrCreate, FileAccess.Write));
            if (writeHeaderData)
            {
                fileWriter.WriteLine($"\"{string.Join("\",\"", data.First().GetOutputFields())}\"");
            }

            foreach (var item in data)
            {
                if (item == null)
                {
                    continue;
                }

                fileWriter.WriteLine(item.ToCsvLine());
            }
        }

        private void WriteResultsToSql(ICollection<ITestResult> data, string tableName,
            Dictionary<string, string> config)
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

            var useFileBuffer = ((config.ContainsKey("UseFileBuffer") ? config["UseFileBuffer"] : null) ?? string.Empty).ToLower() == "true";
            var connectionString = AppConfiguration.GetConnectionString(config);
            using var dbConnection = new NpgsqlConnection(connectionString);
            try
            {
                dbConnection.Open();
            }
            catch (Exception)
            {
                Logger?.LogError($"Invalid database connection string: [{connectionString}]");
                if (useFileBuffer)
                {
                    WriteResultsToBufferFile(data, tableName, "sql");
                }

                throw;
            }

            var bufferedData = ReadResultsFromBufferFile(tableName, "sql");
            if ((bufferedData?.Count ?? 0) > 0)
            {
                foreach (var item in bufferedData)
                {
                    data.Add(item);
                }
            }
            
            var queryString = $"insert into \"{dbSchema}\".\"{tableName}\" (\"{string.Join("\",\"", data.First().GetOutputFields())}\") values ";
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

        private void WriteResultsToGoogleSpreadsheets(ICollection<ITestResult> data, string sheetName,
            Dictionary<string, string> config)
        {
            if ((data?.Count ?? 0) == 0)
            {
                return;
            }

            var gSheets = new GSheets(config, AppConfiguration.AppPath);
            if (!gSheets.CreateSheetIfMissing(sheetName))
            {
                throw new Exception($"Unable to create Google spreadsheet sheet [{sheetName}]!");
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

            var listItem = new List<object> {rNo + 1, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")};
            var range = $"A{rNo + 2}:{GSheets.GetNextLetter('B', data.Count * 2)}{rNo + 2}";
            listItem.AddRange(data.Select(item => item?.GetResult()));
            listItem.AddRange(data.Select(item => item == null ? "N/A" : item.GetMessage()));

            gSheets.WriteRange(new List<IList<object>> {listItem}, range, sheetName);
        }

        protected virtual void WriteResultsToBufferFile(IEnumerable<ITestResult> data, string collectionName,
            string sourceName)
        {
            if (!Directory.Exists(Path.Join(AppConfiguration.AppPath, "data", sourceName)))
            {
                Directory.CreateDirectory(Path.Join(AppConfiguration.AppPath, "data", sourceName));
            }

            var fileName = Path.Join(AppConfiguration.AppPath, "data", sourceName, collectionName + ".temp");
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

        protected virtual ICollection<ITestResult> ReadResultsFromBufferFile(string collectionName, string sourceName)
        {
            var fileName = Path.Join(AppConfiguration.AppPath, "data", sourceName, collectionName + ".temp");
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
                        ITestResult item;
                        if ((item = DeserializeBufferedItem(line)) != null)
                        {
                            data.Add(item);
                        }
                    }
                }

                File.Delete(fileName);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Unable to read buffered data from file: [{fileName}]");
                data = null;
            }

            return data;
        }

        protected abstract ITestResult DeserializeBufferedItem(string data);
    }
}