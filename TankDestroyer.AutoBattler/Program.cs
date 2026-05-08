using Spectre.Console;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TankDestroyer.API;
using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler;

class Program
{
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

            var maps = CollectMapsService.LoadMaps(mapFolder);
            if (maps.Length == 0)
            {
                AnsiConsole.MarkupLine($"[red]No maps found in:[/] {mapFolder}");
                return;
            }

            var bots = botTypes.Select(type => (IPlayerBot)Activator.CreateInstance(type)!);
            var botPairs = bots.SelectMany(x => bots.Where(y => y != x), (x, y) => (First: x, Second: y)).ToList();
            var games = maps.SelectMany(map => botPairs, (map, botPair) => new { Map = map, BotPair = botPair });

            await Parallel.ForEachAsync(
                games,
                async (game, ct) =>
                {
                    await RunGame([game.BotPair.First, game.BotPair.Second], game.Map);
                }
            );

            AnsiConsole.MarkupLine("[bold green]Game Finished![/]");
        }
        finally
        {
            AnsiConsole.Cursor.Show();
        }
    }

    private static async Task RunGame(IPlayerBot[] bots, World selectedMap)
    {
        var playerColors = new Dictionary<int, Color>();
        var playerLabels = new Dictionary<int, string>();
        for (var i = 0; i < bots.Length; i++)
        {
            var attribute = bots[i].GetType().GetCustomAttribute<BotAttribute>();
            var colorHex = attribute?.Color ?? "#808080";

            if (!Color.TryFromHex(colorHex, out var color))
            {
                color = Color.Grey;
            }

            playerColors[i] = color;
            playerLabels[i] = attribute?.Name ?? bots[i].GetType().Name;
        }

        var runner = new GameRunner(selectedMap, bots);

        GameTurn? previousTurn = null;
        var initialTurn = runner.GetTurns().Last();
        previousTurn = initialTurn;

        while (!runner.Finished)
        {
            for (var turnIndex = 0; !runner.Finished; turnIndex++)
            {
                runner.DoTurn();
                var lastTurn = runner.GetTurns().Last();
                previousTurn = lastTurn;
            }
        }
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

    static string GetDeterministicHexColor(string input)
    {
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        // Use the first 3 bytes to create a valid RRGGBB hex color
        return $"{hashBytes[0]:X2}{hashBytes[1]:X2}{hashBytes[2]:X2}";
    }

    private static int AskTurnsToPlay()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How many [green]turns[/] to play?")
                .AddChoices(new[] { "1 turn", "X turns", "All remaining", "Quit" })
        );

        switch (choice)
        {
            case "1 turn":
                return 1;
            case "X turns":
                return AnsiConsole.Ask<int>("Number of turns:");
            case "All remaining":
                return int.MaxValue;
            case "Quit":
            default:
                return 0;
        }
    }

    private class AppConfig
    {
        public string BotFolder { get; set; } = "..\\Bots";
        public string MapFolder { get; set; } = "..\\Maps";
    }
}
