using Spectre.Console;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using TankDestroyer.API;
using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler;

class Program
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

            var config = LoadConfig();
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

            var botTypes = CollectBotsServices.LoadBots(botFolder);
            if (botTypes.Length == 0)
            {
                AnsiConsole.MarkupLine($"[red]No bots found in:[/] {botFolder}");
                return;
            }

            var botMetadata = botTypes.ToDictionary(
                type => type,
                type =>
                {
                    var attr = type.GetCustomAttribute<BotAttribute>();
                    var name = attr?.Name ?? type.Name;
                    var color = attr?.Color;

                    if (string.IsNullOrWhiteSpace(color) || !Color.TryFromHex(color, out _))
                    {
                        color = GetDeterministicHexColor(name);
                    }

                    if (!color.StartsWith("#"))
                        color = "#" + color;

                    return new BotInfo
                    {
                        Name = name,
                        Creator = attr?.Creator ?? "Unknown",
                        Color = color,
                    };
                }
            );

            var mapFilter = args.Length > 0 ? args[0] : null;

            var maps = CollectMapsService.LoadMaps(mapFolder);
            if (!string.IsNullOrEmpty(mapFilter))
            {
                maps = maps.Where(m => m.Name.Contains(mapFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
            }

            if (maps.Length == 0)
            {
                AnsiConsole.MarkupLine(
                    $"[red]No maps found in:[/] {mapFolder}{(string.IsNullOrEmpty(mapFilter) ? "" : $" matching '{mapFilter}'")}"
                );
                return;
            }

            var botGroups = botTypes.SelectMany(x => botTypes.Where(y => y != x), (x, y) => new[] { x, y }).ToList();
            var games = maps.SelectMany(map => botGroups, (map, group) => new { Map = map, BotTypes = group }).ToList();

            Console.SetOut(TextWriter.Null);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await AnsiConsole
                .Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(OriginalOut) })
                .Progress()
                .AutoRefresh(true)
                .Columns(
                    [
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new ItemCountColumn(),
                        new RemainingTimeColumn(),
                        new SpinnerColumn(Spinner.Known.Dots),
                    ]
                )
                .StartAsync(async ctx =>
                {
                    var progressTask = ctx.AddTask("[green]Simulating Games[/]", true, games.Count);

                    await Parallel.ForEachAsync(
                        games,
                        async (game, ct) =>
                        {
                            var botInstances = game.BotTypes.Select(type => (IPlayerBot)Activator.CreateInstance(type)!).ToArray();
                            var gameBotInfos = new List<BotInfo>();
                            for (int i = 0; i < game.BotTypes.Length; i++)
                            {
                                var info = botMetadata[game.BotTypes[i]];
                                gameBotInfos.Add(
                                    new BotInfo
                                    {
                                        OwnerId = i,
                                        Name = info.Name,
                                        Creator = info.Creator,
                                        Color = info.Color,
                                    }
                                );
                            }

                            await RunGame(botInstances, game.Map, gameBotInfos.ToArray());
                            progressTask.Increment(1);
                        }
                    );
                });

            sw.Stop();

            var totalStalemates = GameResults.Count(r => r.IsStalemate);
            Console.SetOut(OriginalOut);
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

    private static async Task RunGame(IPlayerBot[] bots, World selectedMap, BotInfo[] botInfos)
    {
        var runner = new GameRunner(selectedMap, bots);
        GameTurn? lastTurn = null;
        int turnCount = 0;
        bool hasCrashed = false;
        bool isStalemate = false;

        try
        {
            for (; turnCount < MaxTurnsForStaleMate && !runner.Finished; turnCount++)
            {
                runner.DoTurn();
            }

            lastTurn = runner.GetTurns().Last();
        }
        catch (Exception e)
        {
            hasCrashed = true;
            lastTurn = runner.GetTurns().LastOrDefault();
        }

        GameResults.Add(
            new GameResult()
            {
                MapName = selectedMap.Name,
                Bots = lastTurn?.Tanks.ToList() ?? [],
                BotInfo = botInfos.ToList(),
                TurnsPlayed = turnCount,
                HasCrashed = hasCrashed,
                IsStalemate = isStalemate || turnCount >= MaxTurnsForStaleMate,
            }
        );
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
