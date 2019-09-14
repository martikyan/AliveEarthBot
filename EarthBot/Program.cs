using System.IO;
using System.Linq;
using EarthBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EarthBot
{
    internal class Program
    {
        public static IConfiguration Configuration { get; set; }

        private static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var botService = serviceProvider.GetService<BotService>();

            botService.Init(daemonModeEnabled: args.Contains("--daemon"));
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            services
                .AddSingleton<BotService>()
                .AddScoped<IConfiguration>(config => builder.Build())
                .AddLogging(configure =>
                {
                    configure.AddConsole();
                    configure.SetMinimumLevel(LogLevel.Information);
                });
        }
    }
}