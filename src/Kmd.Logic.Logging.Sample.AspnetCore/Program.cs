using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Kmd.Logic.Logging.Sample.AspnetCore
{
    public class Program
    {
        static string EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        static readonly string ConsoleMinLevelKey = "Logging:ConsoleMinLevel";

        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static int Main(string[] args)
        {
            var config = Configuration;
            // When we are in local development mode, we want to be able to use the console to read the
            // logs, however in non-development environments, we don't want the console to cause performance
            // or IO issues, so we make sure only fatal events are recorded there.
            var consoleMinLevel = config.GetValue<LogEventLevel>(ConsoleMinLevelKey, LogEventLevel.Information);
            var seqServerUrl = config.GetValue<string>("Logging:Seq:ServerUrl", null);
            var seqApiKey = config.GetValue<string>("Logging:Seq:ApiKey", null);

            // Just like the regular output template, but includes {Properties} so that during development,
            // you can more easily become aware of the contextual correlation properties begin added to
            // all your log events.
            var consoleOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <props: {Properties}>{NewLine}{Exception}";
            
            // Use a logging level switch so that Seq can report back a new logging level remotely
            // based on the API key.
            var seqLogLevelSwitch = new LoggingLevelSwitch();

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty(name: "App", value: PlatformServices.Default.Application.ApplicationName)
                .Enrich.WithProperty(name: "Version", value: typeof(Program).Assembly.GetName().Version.ToString())
                .WriteTo.Console(outputTemplate: consoleOutputTemplate, restrictedToMinimumLevel: consoleMinLevel)
                .WriteTo.Seq(serverUrl: seqServerUrl, apiKey: seqApiKey, controlLevelSwitch: seqLogLevelSwitch, compact: true)
                // Focus on the logs from our application, not the aspnet core infrastructure
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .CreateLogger();

            try
            {
                Log.Information("Getting the motors running...");

                CreateWebHostBuilder(args).UseConfiguration(config).UseSerilog().Build().Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.Information("Flushing and shutting down");
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
