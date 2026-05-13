using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TankDestroyer.AutoBattler.API.Dto;
using TankDestroyer.AutoBattler.API.Hubs;
using TankDestroyer.AutoBattler.Configuration;
using TankDestroyer.AutoBattler.Objects;
using TankDestroyer.Engine;
using Game = TankDestroyer.AutoBattler.Objects.Game;

namespace TankDestroyer.AutoBattler.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class BattleController(
    ChannelWriter<BattleRequest> battleRequestWriter,
    ChannelReader<GameResult> gameResultReader,
    IHubContext<BattleHub> hubContext,
    Type[] botTypes,
    World[] maps
) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> StartBattle([FromBody] BattleRequestDto request)
    {
        var selectedMaps = string.IsNullOrWhiteSpace(request.MapName)
            ? maps
            : maps.Where(m => m.Name == request.MapName).ToArray();

        if (!selectedMaps.Any())
            return NotFound($"Map '{request.MapName}' not found.");

        var botGroups = botTypes
            .SelectMany(x => botTypes.Where(y => y != x), (x, y) => new[] { x, y })
            .ToList();

        var games = selectedMaps
            .SelectMany(map => botGroups, (map, group) => new Game
            {
                Map = map,
                BotTypes = group
            }).ToList();

        await battleRequestWriter.WriteAsync(new BattleRequest
        {
            MaxTurns = request.Amount,
            Games = games
        });

        _ = Task.Run(async () =>
        {
            await foreach (var result in gameResultReader.ReadAllAsync())
            {
                await hubContext.Clients.All.SendAsync("ReceiveResult", result);
            }
        });

        return Accepted();
    }
}