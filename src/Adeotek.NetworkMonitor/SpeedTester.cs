using System;
using System.Diagnostics;
using Adeotek.NetworkMonitor.Configuration;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor
{
    public class SpeedTester : INetworkTester
    {
        private readonly AppConfiguration _appConfiguration;
        private readonly ILogger _logger;

        public SpeedTester(AppConfiguration appConfiguration, ILogger logger)
        {
            _appConfiguration = appConfiguration;
            _logger = logger;
        }

        public void RunTest()
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
                _logger?.LogDebug($"Speed test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error running speed test!");
            }
        }
    }
}