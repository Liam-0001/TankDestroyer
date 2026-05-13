using Microsoft.AspNetCore.SignalR;
using TankDestroyer.AutoBattler.Objects;

namespace TankDestroyer.AutoBattler.API.Hubs;

public class BattleHub : Hub
{
    public async Task StreamResults(IAsyncEnumerable<GameResult> stream)
    {
        await foreach (var result in stream)
        {
            await Clients.All.SendAsync("ReceiveResult", result);
        }
    }
}