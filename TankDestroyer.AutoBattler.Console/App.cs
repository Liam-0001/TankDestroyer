using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using TankDestroyer.AutoBattler.Console.Configuration;
using TankDestroyer.AutoBattler.Objects;
using TankDestroyer.Engine;
using TankDestroyer.Engine.Services.Instantiate;
using Game = TankDestroyer.AutoBattler.Objects.Game;

namespace TankDestroyer.AutoBattler.Console;

public class App(
    ChannelWriter<BattleRequest> battleRequestWriter,
    ChannelReader<GameResult> gameResultReader,
    IHostApplicationLifetime lifetime,
    BattleConfiguration battleConfiguration,
    Type[] botTypes,
    World[] maps
) : IApp
{
    private static readonly ConcurrentBag<GameResult> GameResults = [];
    private static readonly TextWriter OriginalOut = System.Console.Out;

    public async Task RunAsync()
    {
        System.Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.Cursor.Hide();

        try
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[blue]Loading application[/]");

            var botGroups = botTypes.SelectMany(x => botTypes.Where(y => y != x), (x, y) => new[] { x, y }).ToList();
            var games = maps.SelectMany(map => botGroups, (map, group) => new { Map = map, BotTypes = group }).ToList();

            var visibleConsole = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(OriginalOut)
            });
            System.Console.SetOut(TextWriter.Null);
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
                        await foreach (var result in gameResultReader.ReadAllAsync())
                        {
                            GameResults.Add(result);
                            progressTask.Increment(1);
                        }
                    });

                    await battleRequestWriter.WriteAsync(new BattleRequest
                    {
                        MaxTurns = battleConfiguration.MaxTurnsForStaleMate,
                        Games = games.Select(g => new Game
                        {
                            Map = g.Map,
                            BotTypes = g.BotTypes
                        }).ToList()
                    });

                    battleRequestWriter.Complete();
                    await resultConsumer;
                    lifetime.StopApplication();
                });

            sw.Stop();

            System.Console.SetOut(OriginalOut);
            var totalStalemates = GameResults.Count(r => r.IsStalemate);
            AnsiConsole.MarkupLine($"[green]Simulations finished in {sw.Elapsed.TotalSeconds:F2}s[/]");
            AnsiConsole.MarkupLine($"[yellow]Total stalemates:[/] {totalStalemates}");

            var renderer = new ResultRenderer();
            renderer.PrintResults(GameResults.ToList());

            AnsiConsole.MarkupLine("[bold green]Game Finished![/]");
        }
        finally
        {
            System.Console.SetOut(OriginalOut);
            AnsiConsole.Cursor.Show();
        }
    }

    private static string ResolvePath(string? configuredPath, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(configuredPath) ? fallback : configuredPath;
        value = value.Replace('\\', Path.DirectorySeparatorChar);
        var path = Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
        return path;
    }
}