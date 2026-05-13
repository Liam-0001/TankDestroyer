using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler.Objects;

public class GameResult
{
    public string MapName { get; set; } = string.Empty;
    public List<Tank> Bots { get; set; } = [];
    public List<BotInfo> BotInfo { get; set; } = [];
    public int TurnsPlayed { get; set; }
    public bool HasCrashed { get; set; }
    public bool IsStalemate { get; set; }
}