using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Adeotek.NetworkMonitor.Configuration;
using Adeotek.NetworkMonitor.Results;
using Adeotek.NetworkMonitor.Writers;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor.Testers
{
    public class UptimeTester : ITester
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public UptimeTester(AppConfiguration appConfiguration, ILogger logger)
        {
            _appConfiguration = appConfiguration;
            _logger = logger;
        }

        public void Run(TestConfiguration test)
        {
            _logger?.LogDebug("Starting uptime test...");
            if ((test?.Targets?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No uptime targets found!");
                return;
            }

            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var results = new List<ITestResult>();
                foreach (var target in test.Targets.Where(target => target?.ContainsKey("Url") ?? false))
                {
                    results.Add(DoTest(target["Url"], test.Group, target.ContainsKey("Name") ? target["Name"] : null));
                }

                WriteTestResults(results, test.Collection, test.Group);
                
                timer.Stop();
                _logger?.LogInformation($"Uptime test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running uptime test!");
            }
        }
        
        public ICollection<ITestResult> RunAndReturn(TestConfiguration test)
        {
            _logger?.LogDebug("Starting uptime test...");
            if ((test?.Targets?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No uptime targets found!");
                return null;
            }
            
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var results = new List<ITestResult>();
                foreach (var target in test.Targets.Where(target => target?.ContainsKey("Url") ?? false))
                {
                    results.Add(DoTest(target["Url"], test.Group, target.ContainsKey("Name") ? target["Name"] : null));
                }
                timer.Stop();
                _logger?.LogInformation($"Uptime test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
                return results;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running uptime test!");
                return null;
            }
        }

        public UptimeResult DoTest(string url, string group = null, string name = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                return new UptimeResult
                {
                    Success = false,
                    Group = group,
                    Name = name,
                    Url = url,
                    Code = 0,
                    Message = "Invalid URL"
                };
            }

            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
                var response = httpClient.GetAsync(url).GetAwaiter().GetResult();
                
                return new UptimeResult
                {
                    Success = response.StatusCode == HttpStatusCode.OK,
                    Group = group,
                    Name = name,
                    Url = url,
                    Code = (int) response.StatusCode,
                    Message = Enum.GetName(typeof(HttpStatusCode), response.StatusCode)
                };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Unable to process uptime web request to URL: [{url}]");
                return new UptimeResult
                {
                    Success = false,
                    Group = group,
                    Name = name,
                    Url = url,
                    Code = 0,
                    Message = e.Message
                };
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
                                new CsvWriter<UptimeResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            case "POSTGRESQL":
                                new PostgreSqlWriter<UptimeResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            case "GOOGLESPREADSHEETS":
                                new GoggleSpreadsheetsWriter<UptimeResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            default:
                                continue;
                        }

                        successfulWrites++;
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Unable to write uptime test results!");
                    }
                }
                
                if (successfulWrites == 0)
                {
                    throw new Exception("All writers failed!");
                }
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, "Unable to write uptime test results!");
                _logger?.LogInformation($"Uptime test results: {JsonSerializer.Serialize(results)}");
            }
        }
    }
}