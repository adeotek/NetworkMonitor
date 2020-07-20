using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Adeotek.NetworkMonitor.Results;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor.Writers
{
    public class CsvWriter<T> : IResultWriter
    {
        private readonly Dictionary<string, string> _config;
        private readonly ILogger _logger;
        private readonly string _appPath;
        
        public CsvWriter(Dictionary<string, string> config, ILogger logger, string appPath)
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
                throw new Exception("Null or empty CSV file name!");
            }
            
            var path = _config.ContainsKey("Path") ? _config["Path"] : null;
            string csvFile;
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path))
            {
                csvFile = Path.Join(_appPath, collection + ".csv");
            }
            else
            {
                if (!Directory.Exists(Path.Join(_appPath, path)))
                {
                    Directory.CreateDirectory(Path.Join(_appPath, path));
                }

                csvFile = Path.Join(_appPath, path, collection + ".csv");
            }

            var writeHeaderData = !File.Exists(csvFile);
            using var fileWriter = new StreamWriter(new FileStream(csvFile, FileMode.OpenOrCreate, FileAccess.Write));
            if (writeHeaderData)
            {
                fileWriter.WriteLine($"\"{string.Join("\",\"", data.First().GetFields())}\"");
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
    }
}