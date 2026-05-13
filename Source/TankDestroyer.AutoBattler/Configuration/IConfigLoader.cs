namespace TankDestroyer.AutoBattler.Configuration;

public interface IConfigLoader
{
    public InitialGameObject? LoadConfig();
}