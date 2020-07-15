using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Adeotek.NetworkMonitor.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration configuration;
            ILogger logger;
            var environmentName = Environment.GetEnvironmentVariable("DOTNET_CORE_ENVIRONMENT");

            try
            {
                configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args)
                    .Build();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to load configuration: {e.Message}");
                return;
            }

            try
            {
                var sLogger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

                var services = new ServiceCollection();
                services.AddLogging(configure => configure.AddSerilog(sLogger, true));
                var provider = services.BuildServiceProvider();
                logger = provider.GetService<ILoggerFactory>().CreateLogger("Application");
                logger.LogDebug("Logger initialized.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to create logger object: {e.Message}");
                logger = null;
            }

            try
            {
                var networkMonitor = new NetworkMonitor(configuration, logger);
                networkMonitor.RunAllTests();
            }
            catch (Exception e)
            {
                logger?.LogError(e, "NetworkMonitor.RunAllTest failed!");
            }

            if (environmentName == "Development")
            {
                Console.ReadKey();
            }
        }
    }
}
