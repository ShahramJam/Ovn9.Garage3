using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            .AddSingleton<IGarageHandler, GarageHandler>()
            .AddSingleton<IUI, ConsoleUI>()
            .BuildServiceProvider();

        await services.GetRequiredService<IUI>().RunAsync();
    }
}
