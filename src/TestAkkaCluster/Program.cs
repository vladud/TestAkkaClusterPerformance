using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestAkkaClusterPerformance
{
    public class Program
    {
        public static ILoggerFactory LogFactory { get; private set; }

        public static async Task Main(string[] args)
        {
            LogFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            var host = new HostBuilder()
                .ConfigureAppConfiguration(configHost =>
                {
                    configHost.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
                    configHost.AddJsonFile("defaultTestParams.json", optional: true);
                    configHost.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();
                    services.AddSingleton<TestParams>();
                    services.AddHostedService<AkkaService>(); // runs Akka.NET
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();
                })
                .UseConsoleLifetime()
                .Build();

            await host.RunAsync();
        }
    }
}
