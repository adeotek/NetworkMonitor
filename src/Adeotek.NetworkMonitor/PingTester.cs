using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Adeotek.NetworkMonitor.Configuration;
using Microsoft.Extensions.Logging;

namespace Adeotek.NetworkMonitor
{
    public class PingTester : NetworkTester, INetworkTester
    {
        public PingTester(AppConfiguration appConfiguration, ILogger logger) : base(appConfiguration, logger)
        {
        }

        public void RunTest()
        {
            Logger?.LogDebug("Starting ping test...");
            if ((AppConfiguration?.PingTest?.Targets?.Count ?? 0) == 0)
            {
                Logger?.LogWarning("No ping targets found!");
                return;
            }

            try
            {
                var timer = new Stopwatch();
                timer.Start();
                var pinger = new Pinger();
                var results = AppConfiguration.PingTest.Targets.Select(target => pinger.SafePing(target)).ToList();
                var resultsWrite = WriteTestResults(results, AppConfiguration.PingTest?.WriteToCollection);
                if (!resultsWrite)
                {
                    Logger?.LogInformation($"Ping test results: {JsonSerializer.Serialize(results)}");
                }

                timer.Stop();
                Logger?.LogInformation($"Ping test done in {timer.ElapsedMilliseconds / 1000:#0.000} sec.");
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Error running ping test!");
            }
        }

        protected override ITestResult DeserializeBufferedItem(string data)
        {
            try
            {
                return JsonSerializer.Deserialize<PingResult>(data);
            }
            catch (Exception e)
            {
                Logger?.LogWarning(e, $"Unable to convert JSON string to PingResult: {data}");
                return null;
            }
        }
    }
}