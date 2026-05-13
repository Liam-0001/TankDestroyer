using System.Threading.Channels;
using TankDestroyer.AutoBattler.Configuration;
using TankDestroyer.AutoBattler.Objects;
using TankDestroyer.AutoBattler.Services;
using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler.API.Extensions;

public static class ServiceCollectionsExtensions
{
    public static IServiceCollection AddBattleInfrastructure(this IServiceCollection services,
        Type[] botTypes,
        World[] maps)
    {
        var battleRequestChannel = Channel.CreateBounded<BattleRequest>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        var gameResultChannel = Channel.CreateUnbounded<GameResult>();

        services.AddSingleton(battleRequestChannel.Reader);
        services.AddSingleton(battleRequestChannel.Writer);
        services.AddSingleton(gameResultChannel.Reader);
        services.AddSingleton(gameResultChannel.Writer);
        services.AddSingleton(new BattleConfiguration { MaxTurnsForStaleMate = 200 });
        services.AddSingleton(botTypes);
        services.AddSingleton(maps);
        services.AddHostedService<BattleService>();

        return services;
    }
}