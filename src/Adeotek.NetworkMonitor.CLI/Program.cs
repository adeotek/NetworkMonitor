using System;
using System.Linq;
using Adeotek.NetworkMonitor.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Adeotek.NetworkMonitor.CLI
{
    internal class Program
    {
        private static string _environmentName;
        private static IConfiguration _configuration;
        private static AppConfiguration _appConfiguration;
        private static ILogger _logger;

        private static void Main(string[] args)
        {
            SetEnvironmentName(args);
            Console.WriteLine(
                $"Starting network monitor CLI {(_environmentName == "Development" ? "(Development) " : string.Empty)}...");
            if (!LoadConfiguration(args))
            {
                return;
            }

            SetupLogger();

            try
            {
                var networkMonitor = new NetworkMonitor(_appConfiguration, _logger);
                networkMonitor.RunTests();
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
                if (arg.ToUpper().StartsWith("DOTNETCORE_ENVIRONMENT=") && arg.Length > 24)
                {
                    _environmentName = arg.Substring(23);
                }
                else if (arg.ToUpper().StartsWith("ENVIRONMENT=") && arg.Length > 13)
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
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile($"appsettings.{_environmentName}.json", true, true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build();

                _appConfiguration = _configuration.GetSection("Application").Get<AppConfiguration>();
                if (_appConfiguration == null)
                {
                    throw new Exception("Invalid or missing [Application] configuration section!");
                }

                _appConfiguration.AppPath = AppDomain.CurrentDomain.BaseDirectory;
                if (args.Select(a => a.ToLower() == "--do-ping-test").FirstOrDefault() && _appConfiguration.PingTest != null)
                {
                    _appConfiguration.PingTest.Enabled = true;
                }

                if (args.Select(a => a.ToLower() == "--do-speed-test").FirstOrDefault() && _appConfiguration.SpeedTest != null)
                {
                    _appConfiguration.SpeedTest.Enabled = true;
                }

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
                using var sLogger = new LoggerConfiguration().ReadFrom.Configuration(_configuration).CreateLogger();
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