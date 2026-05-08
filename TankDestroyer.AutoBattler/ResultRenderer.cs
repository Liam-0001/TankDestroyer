using Spectre.Console;

namespace TankDestroyer.AutoBattler;

public class ResultRenderer
{
    private readonly IAnsiConsole _console;

    public ResultRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void PrintResults(IEnumerable<GameResult> results)
    {
        var maps = results.Select(r => r.MapName).Distinct().ToList();

        if (maps.Count > 1)
        {
            _console.MarkupLine($"[bold blue]Total[/]");
            PrintStatsTable(results);
            _console.WriteLine();
        }

        foreach (var map in maps)
        {
            _console.MarkupLine($"[bold blue]{map}[/]");
            PrintStatsTable(results.Where(r => r.MapName == map));
            _console.WriteLine();
        }
    }

    private void PrintStatsTable(IEnumerable<GameResult> results)
    {
        var botStats = new Dictionary<string, (int Wins, int Losses, int Draws, int Stalemates, int Crashes, string Color)>();

        foreach (var result in results)
        {
            var survivors = result.Bots.Where(t => !t.Destroyed).ToList();
            var isStalemate = result.IsStalemate;

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

        _console.Write(table);
    }
}
