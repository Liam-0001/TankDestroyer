using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using TankDestroyer.AutoBattler.Console;
using TankDestroyer.AutoBattler.Console.Configuration;
using TankDestroyer.AutoBattler.Console.Extensions;
using TankDestroyer.AutoBattler.Objects;
using TankDestroyer.AutoBattler.Services;
using TankDestroyer.Engine;
using TankDestroyer.Engine.Services.Instantiate;

var battleRequestChannel = Channel.CreateBounded<BattleRequest>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait
});
var gameResultChannel = Channel.CreateUnbounded<GameResult>();

var botService = new CollectBotsService();
var mapService = new CollectMapsService();

var botFolder = ResolvePath("..\\Build\\Bots", "..\\Bots");
var mapFolder = ResolvePath("..\\Maps", "..\\Maps");

var botTypes = botService.LoadBots(botFolder);
var maps = mapService.LoadMaps(mapFolder);

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddLoadConfiguration();

    services.AddSingleton(battleRequestChannel.Reader);
    services.AddSingleton(battleRequestChannel.Writer);
    services.AddSingleton(gameResultChannel.Reader);
    services.AddSingleton(gameResultChannel.Writer);
    services.AddSingleton(new BattleConfiguration { MaxTurnsForStaleMate = 200 });

    services.AddSingleton(botTypes);
    services.AddSingleton(maps);

    services.AddHostedService<BattleService>();
    services.AddTransient<IApp, App>();
});

var host = builder.Build();

await host.StartAsync();

await host.Services
    .GetRequiredService<IApp>()
    .RunAsync();

static string ResolvePath(string? configuredPath, string fallback)
{
    var value = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath;
    value = value.Replace('\\', Path.DirectorySeparatorChar);
    return Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
}