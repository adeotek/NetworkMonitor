using System;
using System.Diagnostics;
using Adeotek.NetworkMonitor.Configuration;
using Adeotek.NetworkMonitor.Testers;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor
{
    public class NetworkMonitor
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public NetworkMonitor(AppConfiguration appConfiguration, ILogger logger)
        {
            _appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            _logger = logger;
            _logger?.LogInformation("NetworkMonitor initialized.");
        }

        public void RunTests()
        {
            if ((_appConfiguration.Tests?.Count ?? 0) == 0)
            {
                _logger?.LogWarning("No tests configured!");
                return;
            }
            
            _logger?.LogDebug("Starting tests...");
            var timer = new Stopwatch();
            timer.Start();

            foreach (var test in _appConfiguration.Tests)
            {
                switch (test?.Type)
                {
                    case "Ping":
                        new PingTester(_appConfiguration, _logger).Run(test);
                        break;
                    case "Uptime":
                        new UptimeTester(_appConfiguration, _logger).Run(test);
                        break;
                    case "OpenedPort":
                        new OpenedPortTester(_appConfiguration, _logger).Run(test);
                        break;
                    case "Speed":
                        _logger?.LogWarning($"[{test.Type}] NOT IMPLEMENTED YET!!!");
                        break;
                    default:
                        _logger?.LogWarning($"Invalid test type: [{test?.Type ?? string.Empty}]");
                        break;
                }
            }

            timer.Stop();
            _logger?.LogDebug($"Tests done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
        }
    }
}