using Spectre.Console;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TankDestroyer.API;
using TankDestroyer.AutoBattler.Objects;
using TankDestroyer.AutoBattler.Services;
using TankDestroyer.Engine;
using TankDestroyer.Engine.Services.Instantiate;
using Game = TankDestroyer.AutoBattler.Objects.Game;

namespace TankDestroyer.AutoBattler;

static class Program
{
    public const int MaxTurnsForStaleMate = 200;
    public static ConcurrentBag<GameResult> GameResults = [];
    private static readonly TextWriter OriginalOut = Console.Out;

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.Cursor.Hide();

        try
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[blue]Loading application[/]");

            var botFolder = ResolvePath("..\\Build\\Bots", "..\\Bots");
            var mapFolder = ResolvePath("..\\Maps", "..\\Maps");

            if (!Directory.Exists(botFolder))
            {
                AnsiConsole.MarkupLine($"[red]Bot folder not found:[/] {botFolder}");
                return;
            }

            if (!Directory.Exists(mapFolder))
            {
                AnsiConsole.MarkupLine($"[red]Map folder not found:[/] {mapFolder}");
                return;
            }

            var botService = new CollectBotsService();
            var botTypes = botService.LoadBots(botFolder);
            if (botTypes.Length == 0)
            {
                AnsiConsole.MarkupLine($"[red]No bots found in:[/] {botFolder}");
                return;
            }

            var mapService = new CollectMapsService();
            var maps = mapService.LoadMaps(mapFolder);

            if (maps.Length == 0)
            {
                AnsiConsole.MarkupLine($"[red]No maps found in:[/] {mapFolder}");
                return;
            }

            var botGroups = botTypes.SelectMany(x => botTypes.Where(y => y != x), (x, y) => new[] { x, y }).ToList();
            var games = maps.SelectMany(map => botGroups, (map, group) => new { Map = map, BotTypes = group }).ToList();

            // Channels aanmaken VOOR de host
            var battleRequestChannel = Channel.CreateBounded<BattleRequest>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
            var gameResultChannel = Channel.CreateUnbounded<GameResult>();

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    // Dezelfde channel instances meegeven aan DI
                    services.AddSingleton(battleRequestChannel.Reader);
                    services.AddSingleton(gameResultChannel.Writer);

                    // botTypes en maps meegeven zodat BattleService ze krijgt
                    services.AddSingleton(botTypes);
                    services.AddSingleton(maps);

                    services.AddHostedService<BattleService>();
                })
                .Build();

            await host.StartAsync();

            var visibleConsole =
                AnsiConsole.Create(new AnsiConsoleSettings
                {
                    Out = new AnsiConsoleOutput(OriginalOut)
                });
            Console.SetOut(TextWriter.Null);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            await visibleConsole
                .Progress()
                .AutoRefresh(true)
                .Columns([
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new ItemCountColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn(Spinner.Known.Dots),
                ])
                .StartAsync(async ctx =>
                {
                    var progressTask = ctx.AddTask("[green]Simulating Games[/]", true, games.Count);
                    var resultConsumer = Task.Run(async () =>
                    {
                        await foreach (var result in gameResultChannel.Reader.ReadAllAsync())
                        {
                            GameResults.Add(result);
                            progressTask.Increment(1);
                        }
                    });

                    await battleRequestChannel.Writer.WriteAsync(new BattleRequest
                    {
                        MaxTurns = MaxTurnsForStaleMate,
                        Games = games.Select(g => new Game
                        {
                            Map = g.Map,
                            BotTypes = g.BotTypes
                        }).ToList()
                    });

                    battleRequestChannel.Writer.Complete();
                    await resultConsumer;

                    await host.StopAsync();
                });

            sw.Stop();

            Console.SetOut(OriginalOut);
            var totalStalemates = GameResults.Count(r => r.IsStalemate);
            AnsiConsole.MarkupLine($"[green]Simulations finished in {sw.Elapsed.TotalSeconds:F2}s[/]");
            AnsiConsole.MarkupLine($"[yellow]Total stalemates:[/] {totalStalemates}");

            var renderer = new ResultRenderer();
            renderer.PrintResults(GameResults);

            AnsiConsole.MarkupLine("[bold green]Game Finished![/]");
        }
        finally
        {
            Console.SetOut(OriginalOut);
            AnsiConsole.Cursor.Show();
        }
    }

    static string GetDeterministicHexColor(string input)
    {
        var hashBytes = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(input));
        return $"{hashBytes[0]:X2}{hashBytes[1]:X2}{hashBytes[2]:X2}";
    }


    private class AppConfig
    {
        public string BotFolder { get; set; } = "..\\Bots";
        public string MapFolder { get; set; } = "..\\Maps";
    }

    private static AppConfig LoadConfig()
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        if (!File.Exists(configPath))
        {
            return new AppConfig();
        }

        var json = File.ReadAllText(configPath);
        return System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    private static string ResolvePath(string? configuredPath, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath;
        value = value.Replace('\\', Path.DirectorySeparatorChar);
        var path = Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
        return path;
    }
}