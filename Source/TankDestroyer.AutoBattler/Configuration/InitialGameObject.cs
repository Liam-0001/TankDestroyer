using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler.Configuration;

public class InitialGameObject
{
    public World[] Worlds { get; set; } = [];
    public Type[] Bots { get; set; } = [];
}