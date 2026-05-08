using TankDestroyer.API;
using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler;

public class GameResult
{
    public string MapName { get; set; } = string.Empty;
    public List<Tank> Bots { get; set; } = [];
    public List<BotInfo> BotInfo { get; set; } = [];
    public int TurnsPlayed { get; set; }
    public bool HasCrashed { get; set; }
}

public class BotInfo
{
    public int OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
