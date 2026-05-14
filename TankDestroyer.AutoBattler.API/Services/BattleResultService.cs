using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using TankDestroyer.AutoBattler.API.Dto;
using TankDestroyer.AutoBattler.API.Extensions;
using TankDestroyer.AutoBattler.API.Hubs;
using TankDestroyer.AutoBattler.Objects;

namespace TankDestroyer.AutoBattler.API.Services;

public class BattleResultService(
    ChannelReader<GameResult> gameResultReader,
    IHubContext<BattleHub> hubContext) : BackgroundService
{
    private readonly ConcurrentDictionary<string, BattleResultDto> _totals = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var result in gameResultReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var dtos = GameResultMapper.ToDto(result);

                foreach (var dto in dtos)
                {
                    var key = $"{dto.BotName}||{dto.MapName}";

                    var updatedDto = _totals.AddOrUpdate(key, dto, (k, existing) => existing with
                    {
                        Wins       = existing.Wins       + dto.Wins,
                        Losses     = existing.Losses     + dto.Losses,
                        Stalemates = existing.Stalemates + dto.Stalemates,
                        Crashes    = existing.Crashes    + dto.Crashes
                    });

                    await hubContext.Clients.All.SendAsync("ReceiveResult", updatedDto, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ChannelClosedException)
            {
                break;
            }
        }
    }
}
