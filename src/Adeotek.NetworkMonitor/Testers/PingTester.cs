using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Adeotek.NetworkMonitor.Configuration;
using Adeotek.NetworkMonitor.Helpers;
using Adeotek.NetworkMonitor.Results;
using Adeotek.NetworkMonitor.Writers;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor.Testers
{
    public class PingTester : ITester
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public PingTester(AppConfiguration appConfiguration, ILogger logger)
        {
            _appConfiguration = appConfiguration;
            _logger = logger;
        }

        public void Run(TestConfiguration test)
        {
            _logger?.LogDebug("Starting ping test...");
            if ((test?.Targets?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No ping targets found!");
                return;
            }

            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var pinger = new Pinger();
                var results = new List<ITestResult>();
                foreach (var target in test.Targets.Where(target => target?.ContainsKey("Host") ?? false))
                {
                    results.Add(pinger.SafePing(target["Host"], test.Group, target.ContainsKey("Name") ? target["Name"] : null));
                }

                WriteTestResults(results, test.Collection, test.Group);
                
                timer.Stop();
                _logger?.LogInformation($"Ping test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running ping test!");
            }
        }
        
        public ICollection<ITestResult> RunAndReturn(TestConfiguration test)
        {
            _logger?.LogDebug("Starting ping test...");
            if ((test?.Targets?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No ping targets found!");
                return null;
            }
            
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var pinger = new Pinger();
                var results = new List<ITestResult>();
                foreach (var target in test.Targets.Where(target => target?.ContainsKey("Host") ?? false))
                {
                    results.Add(pinger.SafePing(target["Host"], test.Group, target.ContainsKey("Name") ? target["Name"] : null));
                }
                timer.Stop();
                _logger?.LogInformation($"Ping test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
                return results;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running ping test!");
                return null;
            }
        }
        
        private void WriteTestResults(ICollection<ITestResult> results, string collection, string group)
        {
            if (string.IsNullOrEmpty(collection))
            {
                throw new Exception("Invalid test results collection!");
            }

            try
            {
                if ((_appConfiguration.WriteDataTo?.Count ?? 0) == 0)
                {
                    throw new Exception("No WriteDataTo items present in configuration!");
                }

                var successfulWrites = 0;
                foreach (var target in _appConfiguration.WriteDataTo)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(target?.Type) || !((IList) AppConfiguration.WriteDataToTypes).Contains(target.Type.ToUpper()))
                        {
                            throw new Exception($"Invalid WriteDataTo item configuration: [{target?.Type ?? string.Empty}]");
                        }

                        switch (target.Type.ToUpper())
                        {
                            case "LOCALCSVFILE":
                                new CsvWriter<PingResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            case "POSTGRESQL":
                                new PostgreSqlWriter<PingResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            case "GOOGLESPREADSHEETS":
                                new GoggleSpreadsheetsWriter<PingResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            default:
                                continue;
                        }

                        successfulWrites++;
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Unable to write ping test results!");
                    }
                }
                
                if (successfulWrites == 0)
                {
                    throw new Exception("All writers failed!");
                }
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, "Unable to write ping test results!");
                _logger?.LogInformation($"Ping test results: {JsonSerializer.Serialize(results)}");
            }
        }
    }
}