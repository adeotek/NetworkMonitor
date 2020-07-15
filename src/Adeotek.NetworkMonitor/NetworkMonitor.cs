using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Adeotek.NetworkMonitor.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor
{
    public class NetworkMonitor
    {
        private readonly IConfiguration _configuration;
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public NetworkMonitor(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _appConfiguration = _configuration.GetSection("Application").Get<AppConfiguration>();
            if (_appConfiguration == null)
            {
                throw new Exception("Invalid or missing [Application] configuration section!");
            }
            _logger?.LogInformation("NetworkMonitor initialized.");
        }

        public void RunAllTests()
        {
            _logger?.LogDebug("Starting all tests...");
            var timer = new Stopwatch();
            timer.Start();
            
            if (_appConfiguration?.Ping.Enabled ?? false)
            {

                RunPingTest();
            }

            if (_appConfiguration?.SpeedTest.Enabled ?? false)
            {
                RunSpeedTest();
            }

            timer.Stop();
            _logger?.LogDebug($"All tests done in {timer.ElapsedMilliseconds/1000:#0.000} sec.!");
        }

        public void RunPingTest()
        {
            _logger?.LogDebug("Starting ping test...");
            if ((_appConfiguration?.Ping?.Targets?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No ping targets found!");
                return;
            }

            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var pinger = new Pinger();
                var results = _appConfiguration.Ping.Targets.Select(target => pinger.Ping(target)).ToList();
                if (_appConfiguration?.Ping.SendToGoogleSpreadsheet ?? false)
                {
                    WritePingData(results);
                }
                else
                {
                    _logger?.LogInformation($"Ping test results: {JsonSerializer.Serialize(results)}");
                }
                timer.Stop();
                _logger?.LogInformation($"Ping test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.!");
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
                _logger?.LogDebug($"Speed test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.!");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running speed test!");
            }
        }

        private int WritePingData(ICollection<PingResult> data)
        {
            var gSheets = new GSheets(_appConfiguration);

            if (!gSheets.CreateSheetIfMissing(_appConfiguration.Ping.GoogleSheetName))
            {
                throw new Exception($"Unable to create Google spreadsheet sheet [{_appConfiguration.Ping.GoogleSheetName}]!");
            }

            if ((data?.Count ?? 0) == 0)
            {
                return 0;
            }

            var rNoData = gSheets.ReadRange("A1:A1", _appConfiguration.Ping.GoogleSheetName);
            if ((rNoData?.Count ?? 0) == 0 || (rNoData[0]?.Count ?? 0) == 0 || string.IsNullOrEmpty(rNoData[0][0].ToString()))
            {
                throw new Exception("Unable to get row number cell data!");
            }

            if (!int.TryParse(rNoData[0][0].ToString()?.Substring(4), out var rNo))
            {
                throw new Exception("Unable to parse row number cell data!");
            }

            var listItem = new List<object> { rNo + 1, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            var range = $"A{rNo + 2}:";
            var lastColumn = 'B';
            
            foreach (var item in data)
            {
                if (item == null || !item.Success)
                {
                    continue;
                }
                listItem.Add(item.Time);
                lastColumn = GetNextLetter(lastColumn);
            }
            var values = new List<IList<object>> { listItem };
            range += lastColumn + (rNo + 2).ToString();

            return gSheets.WriteRange(values, range, _appConfiguration.Ping.GoogleSheetName);
        }

        private static char GetNextLetter(char currentLetter) => currentLetter switch
        {
            'z' => 'a',
            'Z' => 'A',
            _ => (char) (currentLetter + 1)
        };
    }
}
