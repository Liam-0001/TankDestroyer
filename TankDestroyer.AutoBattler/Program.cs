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
    private static readonly object ConsoleLock = new();
    private static readonly IAnsiConsole SystemConsole = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(OriginalOut)
    });

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        SystemConsole.Cursor.Hide();

        try
        {
            SystemConsole.Clear();
            SystemConsole.MarkupLine($"[blue]Loading application[/]");

            var config = LoadConfig();
            var botFolder = ResolvePath("..\\Build\\Bots", "..\\Bots");
            var mapFolder = ResolvePath("..\\Maps", "..\\Maps");

            if (!Directory.Exists(botFolder))
            {
                SystemConsole.MarkupLine($"[red]Bot folder not found:[/] {botFolder}");
                return;
            }

            if (!Directory.Exists(mapFolder))
            {
                SystemConsole.MarkupLine($"[red]Map folder not found:[/] {mapFolder}");
                return;
            }

            var botTypes = CollectBotsServices.LoadBots(botFolder);
            if (botTypes.Length == 0)
            {
                SystemConsole.MarkupLine($"[red]No bots found in:[/] {botFolder}");
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
                    
                    if (!color.StartsWith("#")) color = "#" + color;

                    return new BotInfo
                    {
                        Name = name,
                        Creator = attr?.Creator ?? "Unknown",
                        Color = color,
                    };
                }
            );

            var maps = CollectMapsService.LoadMaps(mapFolder).Where(m => m.Name.Contains("River", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (maps.Length == 0)
            {
                SystemConsole.MarkupLine($"[red]No maps found in:[/] {mapFolder}");
                return;
            }

            var botGroups = botTypes.SelectMany(x => botTypes.Where(y => y != x), (x, y) => new[] { x, y }).ToList();
            var games = maps.SelectMany(map => botGroups, (map, group) => new { Map = map, BotTypes = group }).ToList();

            Console.SetOut(TextWriter.Null);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await SystemConsole.Progress()
                .AutoRefresh(true)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn(Spinner.Known.Dots),
                })
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
                                gameBotInfos.Add(new BotInfo
                                {
                                    OwnerId = i,
                                    Name = info.Name,
                                    Creator = info.Creator,
                                    Color = info.Color,
                                });
                            }

                            await RunGame(botInstances, game.Map, gameBotInfos.ToArray());
                            progressTask.Increment(1);
                        }
                    );
                });

            sw.Stop();

            Console.SetOut(OriginalOut);
            SystemConsole.MarkupLine($"[green]Simulations finished in {sw.Elapsed.TotalSeconds:F2}s[/]");
            PrintResults();
            SystemConsole.MarkupLine("[bold green]Game Finished![/]");
        }
        finally
        {
            Console.SetOut(OriginalOut);
            SystemConsole.Cursor.Show();
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
            /*
            lock (ConsoleLock)
            {
                var prevOut = Console.Out;
                Console.SetOut(OriginalOut);
                SystemConsole.WriteException(e, new ExceptionSettings { Format = ExceptionFormats.ShowLinks });
                Console.SetOut(prevOut);
            }*/
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
            }
        );
    }

    private static void PrintResults()
    {
        var botStats = new Dictionary<string, (int Wins, int Losses, int Draws, int Stalemates, int Crashes, string Color)>();

        foreach (var result in GameResults)
        {
            var survivors = result.Bots.Where(t => !t.Destroyed).ToList();
            var isStalemate = result.TurnsPlayed >= MaxTurnsForStaleMate;

            foreach (var botInfo in result.BotInfo)
            {
                if (!botStats.ContainsKey(botInfo.Name))
                {
                    botStats[botInfo.Name] = (0, 0, 0, 0, 0, botInfo.Color);
                }

                var stats = botStats[botInfo.Name];
                var tank = result.Bots.FirstOrDefault(t => t.OwnerId == botInfo.OwnerId);

                if (result.HasCrashed)
                {
                    stats.Crashes++;
                    stats.Losses++;
                }
                else if (isStalemate && tank != null && !tank.Destroyed)
                {
                    stats.Stalemates++;
                }
                else if (survivors.Count == 1 && survivors[0].OwnerId == botInfo.OwnerId)
                {
                    stats.Wins++;
                }
                else if (tank == null || tank.Destroyed)
                {
                    stats.Losses++;
                }
                else
                {
                    stats.Draws++;
                }

                botStats[botInfo.Name] = stats;
            }
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Bot");
        table.AddColumn(new TableColumn("Wins").Centered());
        table.AddColumn(new TableColumn("Losses").Centered());
        table.AddColumn(new TableColumn("Draws").Centered());
        table.AddColumn(new TableColumn("Stalemates").Centered());
        table.AddColumn(new TableColumn("Crashes").Centered());

        foreach (var entry in botStats.OrderByDescending(x => x.Value.Wins))
        {
            var color = entry.Value.Color;
            if (string.IsNullOrEmpty(color) || !Color.TryFromHex(color, out _))
            {
                color = "white";
            }

            table.AddRow(
                $"[{color}]{Markup.Escape(entry.Key)}[/]",
                entry.Value.Wins.ToString(),
                entry.Value.Losses.ToString(),
                entry.Value.Draws.ToString(),
                entry.Value.Stalemates.ToString(),
                entry.Value.Crashes > 0 ? $"[red]{entry.Value.Crashes}[/]" : "0"
            );
        }

        SystemConsole.Write(table);
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

    private class AppConfig
    {
        public string BotFolder { get; set; } = "..\\Bots";
        public string MapFolder { get; set; } = "..\\Maps";
    }
}
