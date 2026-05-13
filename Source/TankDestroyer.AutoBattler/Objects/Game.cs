using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler.Objects;

public class Game
{
    public World Map { get; set; }
    public Type[] BotTypes { get; set; }
}