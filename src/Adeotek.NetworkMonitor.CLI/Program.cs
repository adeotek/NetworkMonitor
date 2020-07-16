using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Adeotek.NetworkMonitor.CLI
{
    class Program
    {
        private static IConfiguration _configuration;
        private static ILogger _logger;
        
        static void Main(string[] args)
        {
            if (!LoadConfiguration(args))
            {
                return;
            }

            SetupLogger();

            try
            {
                var networkMonitor = new NetworkMonitor(_configuration, _logger);
                networkMonitor.RunAllTests();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "NetworkMonitor.RunAllTest failed!");
            }

            if (_configuration.GetValue("environment",string.Empty) == "Development")
            {
                Console.ReadKey();
            }
        }

        private static bool LoadConfiguration(string[] args)
        {
            try
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to load configuration: {e.Message}");
                return false;
            }
        }

        private static void SetupLogger()
        {
            try
            {
                ServiceCollection services;
                using var sLogger = new LoggerConfiguration().ReadFrom.Configuration(_configuration)
                    .CreateLogger();
                services = new ServiceCollection();
                services.AddLogging(configure => configure.AddSerilog(sLogger, true));

                var provider = services.BuildServiceProvider();
                _logger = provider.GetService<ILoggerFactory>().CreateLogger("Application");
                _logger.LogDebug("Logger initialized.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to create logger object: {e.Message}");
                _logger = null;
            }
        }
    }
}
