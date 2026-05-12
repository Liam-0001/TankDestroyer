using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using TankDestroyer.API;
using TankDestroyer.AutoBattler.Objects;
using TankDestroyer.Engine;
using TankDestroyer.Engine.Services.Instantiate;
using Game = TankDestroyer.AutoBattler.Objects.Game;

namespace TankDestroyer.AutoBattler.Services;

public class BattleService(
    ChannelReader<BattleRequest> battleChannelReader,
    Type[] botTypes,
    World[] maps,
    ChannelWriter<GameResult> gameResultChannelWriter)
    : BackgroundService
{
    private readonly ChannelReader<BattleRequest> _battleChannelReader = battleChannelReader;
    private readonly ChannelWriter<GameResult> _gameResultChannelWriter = gameResultChannelWriter;
    private readonly Type[] _botTypes = botTypes;
    private readonly World[] _maps = maps;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var parallelRequestOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 10,
            CancellationToken = stoppingToken
        };

        var parallelGamesOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 40,
            CancellationToken = stoppingToken
        };

        var botMetadata = _botTypes.ToDictionary(
            type => type,
            type =>
            {
                var attr = type.GetCustomAttribute<BotAttribute>();
                var name = attr?.Name ?? type.Name;
                var color = attr?.Color ?? "#000000";

                if (!color.StartsWith('#'))
                {
                    color = $"#{color}";
                }
                
                return new BotInfo
                {
                    Name = name,
                    Creator = attr?.Creator ?? "Unknown",
                    Color = color,
                };
            }
        );

        await Parallel.ForEachAsync(
            source: _battleChannelReader.ReadAllAsync(stoppingToken),
            parallelOptions: parallelRequestOptions,
            body: async (request, ct) =>
            {
                await Parallel.ForEachAsync(
                    source: request.Games,
                    parallelOptions: parallelGamesOptions,
                    body: async (game, ct) =>
                    {
                        var botInstances = game.BotTypes
                            .Select(type => (IPlayerBot)Activator.CreateInstance(type)!)
                            .ToArray();

                        var botInfo = game.BotTypes
                            .Select(t => botMetadata[t])
                            .Select((info, i) => new BotInfo
                            {
                                OwnerId = i, Name = info.Name, Creator = info.Creator, Color = info.Color,
                            }).ToArray();

                        await RunGame(
                            botInstances,
                            game.Map,
                            botInfo,
                            request.MaxTurns
                        );
                    });
            }
        );

        _gameResultChannelWriter.Complete();
    }

    private async Task RunGame(IPlayerBot[] bots, World selectedMap, BotInfo[] botInfos, int maxTurns)
    {
        var runner = new GameRunner(selectedMap, bots);
        GameTurn? lastTurn = null;
        int turnCount = 0;
        bool hasCrashed = false;
        bool isStalemate = false;

        try
        {
            for (; turnCount < maxTurns && !runner.Finished; turnCount++)
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

        await _gameResultChannelWriter.WriteAsync(
            new GameResult()
            {
                MapName = selectedMap.Name,
                Bots = lastTurn?.Tanks.ToList() ?? [],
                BotInfo = botInfos.ToList(),
                TurnsPlayed = turnCount,
                HasCrashed = hasCrashed,
                IsStalemate = isStalemate || turnCount >= maxTurns,
            }
        );
    }
}