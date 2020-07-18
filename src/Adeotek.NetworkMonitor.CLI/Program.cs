using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Adeotek.NetworkMonitor.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Adeotek.NetworkMonitor.CLI
{
    class Program
    {
        private static string _environmentName;
        private static IConfiguration _configuration;
        private static ILogger _logger;
        
        static void Main(string[] args)
        {
            SetEnvironmentName(args);
            Console.WriteLine($"Starting network monitor CLI {(_environmentName == "Development" ? "(Development) " : string.Empty)}...");
            if (!LoadConfiguration(args))
            {
                return;
            }
            SetupLogger();

            try
            {
                var networkMonitor = new NetworkMonitor(_configuration, _logger, AppDomain.CurrentDomain.BaseDirectory);
                networkMonitor.RunAllTests();
                Console.WriteLine("Exiting network monitor...");
            }
            catch (Exception e)
            {
                if (_logger != null)
                {
                    _logger.LogError(e, "NetworkMonitor.RunAllTest failed!");
                }
                else
                {
                    Console.WriteLine($"NetworkMonitor.RunAllTest failed with error: {e.Message}");
                }
            }

            if (_environmentName == "Development")
            {
                Console.ReadKey();
            }
        }

        private static void SetEnvironmentName(string[] args)
        {
            _environmentName = Environment.GetEnvironmentVariable("DOTNETCORE_ENVIRONMENT");
            foreach (var arg in args)
            {
                if (arg.ToUpper().StartsWith("DOTNETCORE_ENVIRONMENT=") && arg.Length>24)
                {
                    _environmentName = arg.Substring(23);
                } else if (arg.ToUpper().StartsWith("ENVIRONMENT=") && arg.Length > 13)
                {
                    _environmentName = arg.Substring(12);
                }
            }
        }

        private static bool LoadConfiguration(string[] args)
        {
            try
            {
                if (_environmentName == "Development")
                {
                    Console.WriteLine($"Configuration location: {AppDomain.CurrentDomain.BaseDirectory}");
                }
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{_environmentName}.json", true, true)
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
