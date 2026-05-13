using Microsoft.Extensions.DependencyInjection;
using TankDestroyer.AutoBattler.Configuration;
using TankDestroyer.Engine.Services.Instantiate;

namespace TankDestroyer.AutoBattler.Console.Extensions;

public static class ConfigurationServiceExtensions
{
    public static IServiceCollection AddLoadConfiguration(this IServiceCollection services)
    {
        services.AddTransient<IConfigLoader, ConfigLoader>();
        services.AddTransient<ICollectBotService, CollectBotsService>();
        services.AddTransient<ICollectMapsService, CollectMapsService>();

        return services;
    }
}