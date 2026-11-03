using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ovn9.Garage3.Abstractions;
using Ovn9.Garage3.Handlers;
using System;
using System.Threading.Tasks;

namespace Ovn9.Garage3;

public static class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Error);
                logging.AddConsole();
            })
            .AddSingleton<IGarageHandler, GarageHandler>()
            .AddSingleton<IAppRunner, ConsoleAppRunner>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));

        try
        {
            await services.GetRequiredService<IAppRunner>().RunAsync();

        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Garageprogrammet avslutades oväntat på grund av ett fel.");
            throw;
        }
    }
}
