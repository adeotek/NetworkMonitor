using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using Adeotek.NetworkMonitor.Configuration;
using Adeotek.NetworkMonitor.Results;
using Adeotek.NetworkMonitor.Writers;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor.Testers
{
    public class OpenedPortTester : ITester
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public OpenedPortTester(AppConfiguration appConfiguration, ILogger logger)
        {
            _appConfiguration = appConfiguration;
            _logger = logger;
        }

        public void Run(TestConfiguration test)
        {
            _logger?.LogDebug("Starting opened port test...");
            if ((test?.Targets?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No opened port targets found!");
                return;
            }

            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var results = new List<ITestResult>();
                foreach (var target in test.Targets.Where(target => (target?.ContainsKey("Host") ?? false) && (target?.ContainsKey("Port") ?? false)))
                {
                    results.Add(DoTest(target["Host"], target["Port"], test.Group, target.ContainsKey("Name") ? target["Name"] : null));
                }

                WriteTestResults(results, test.Collection, test.Group);
                
                timer.Stop();
                _logger?.LogInformation($"Opened port test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running opened port test!");
            }
        }
        
        public ICollection<ITestResult> RunAndReturn(TestConfiguration test)
        {
            _logger?.LogDebug("Starting opened port test...");
            if ((test?.Targets?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No opened port targets found!");
                return null;
            }
            
            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var results = new List<ITestResult>();
                foreach (var target in test.Targets.Where(target => (target?.ContainsKey("Host") ?? false) && (target?.ContainsKey("Port") ?? false)))
                {
                    results.Add(DoTest(target["Host"], target["Port"], test.Group, target.ContainsKey("Name") ? target["Name"] : null));
                }
                timer.Stop();
                _logger?.LogInformation($"Opened port test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
                return results;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running opened port test!");
                return null;
            }
        }

        public OpenedPortResult DoTest(string host, string port, string group = null, string name = null)
        {
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || !int.TryParse(port, out var portNo) || portNo <= 0)
            {
                return new OpenedPortResult
                {
                    Success = false,
                    Group = group,
                    Name = name,
                    Host = host,
                    Port = 0,
                    Message = $"Invalid host/port: [{host}:{port}]"
                };
            }

            try
            {
                using var tcpClient = new TcpClient();
                tcpClient.Connect(host, portNo);
                return new OpenedPortResult
                {
                    Success = true,
                    Group = @group,
                    Name = name,
                    Host = host,
                    Port = portNo,
                    Message = null
                };
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Unable to open socket connection to: [{host}:{port}]");
                return new OpenedPortResult
                {
                    Success = false,
                    Group = group,
                    Name = name,
                    Host = host,
                    Port = portNo,
                    Message = $"[{port}] {e.Message}"
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
                                new CsvWriter<OpenedPortResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            case "POSTGRESQL":
                                new PostgreSqlWriter<OpenedPortResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            case "GOOGLESPREADSHEETS":
                                new GoggleSpreadsheetsWriter<OpenedPortResult>(target.Configuration, _logger, _appConfiguration.AppPath).WriteResults(results, collection, group);
                                break;
                            default:
                                continue;
                        }

                        successfulWrites++;
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Unable to write opened port test results!");
                    }
                }
                
                if (successfulWrites == 0)
                {
                    throw new Exception("All writers failed!");
                }
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, "Unable to write opened port test results!");
                _logger?.LogInformation($"Opened port test results: {JsonSerializer.Serialize(results)}");
            }
        }
    }
}