using System;
using System.Collections.Generic;
using System.Linq;
using Adeotek.NetworkMonitor.Helpers;
using Adeotek.NetworkMonitor.Results;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor.Writers
{
    public class GoggleSpreadsheetsWriter<T> : IResultWriter
    {
        private readonly Dictionary<string, string> _config;
        private readonly ILogger _logger;
        private readonly string _appPath;
        
        public GoggleSpreadsheetsWriter(Dictionary<string, string> config, ILogger logger, string appPath)
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

            var gSheets = new GSheets(_config, _appPath);
            if (!gSheets.CreateSheetIfMissing(collection))
            {
                throw new Exception($"Unable to create Google spreadsheet sheet [{collection}]!");
            }

            var rNoData = gSheets.ReadRange("A1:A1", collection);
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

            gSheets.WriteRange(new List<IList<object>> {listItem}, range, collection);
        }
    }
}