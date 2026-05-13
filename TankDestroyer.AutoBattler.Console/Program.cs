using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using TankDestroyer.AutoBattler.Console;
using TankDestroyer.AutoBattler.Console.Configuration;
using TankDestroyer.AutoBattler.Console.Extensions;
using TankDestroyer.AutoBattler.Objects;
using TankDestroyer.AutoBattler.Services;
using GameResult = TankDestroyer.AutoBattler.Objects.GameResult;

var builder = Host.CreateDefaultBuilder(args);

var battleRequestChannel = Channel.CreateBounded<BattleRequest>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait
});
var gameResultChannel = Channel.CreateUnbounded<GameResult>();

builder.ConfigureServices(services =>
{
    services.AddLoadConfiguration();

    services.AddSingleton(battleRequestChannel.Reader);
    services.AddSingleton(battleRequestChannel.Writer);
    services.AddSingleton(gameResultChannel.Reader);
    services.AddSingleton(gameResultChannel.Writer);
    services.AddSingleton(new BattleConfiguration { MaxTurnsForStaleMate = 200 });

    services.AddHostedService<BattleService>();
    services.AddTransient<IApp, App>();
});

var host = builder.Build();

await host.Services
    .GetRequiredService<IApp>()
    .RunAsync();