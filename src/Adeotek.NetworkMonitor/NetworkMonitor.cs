using System;
using System.Diagnostics;
using Adeotek.NetworkMonitor.Configuration;
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
            _logger?.LogDebug("Starting tests...");
            var timer = new Stopwatch();
            timer.Start();

            if (_appConfiguration.PingTest?.Enabled ?? false)
            {
                new PingTester(_appConfiguration, _logger).RunTest();
            }

            if (_appConfiguration.SpeedTest?.Enabled ?? false)
            {
                new SpeedTester(_appConfiguration, _logger).RunTest();
            }

            timer.Stop();
            _logger?.LogDebug($"Tests done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
        }

        public void RunAllTests()
        {
            _logger?.LogDebug("Starting all tests...");
            var timer = new Stopwatch();
            timer.Start();
            new PingTester(_appConfiguration, _logger).RunTest();
            new SpeedTester(_appConfiguration, _logger).RunTest();
            timer.Stop();
            _logger?.LogDebug($"All tests done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
        }
    }
}