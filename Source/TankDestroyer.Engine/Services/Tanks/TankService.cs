namespace TankDestroyer.Engine.Services.Tanks;

public class TankService(Game game) : ITankService
{
    private readonly Game _game = game;

    public bool Destroy(Tank tank)
    {
        tank.Destroyed = true;
        return tank.Destroyed;
    }

    public Tank[] GetTanks() => _game.Tanks;
    
    public bool Drown(Tank tank)
    {
        tank.Drowned = true;
        return Destroy(tank);
    }
}