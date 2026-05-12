namespace TankDestroyer.Engine.Services.Tanks;

public interface ITankService
{
    public bool Destroy(Tank tank);
    Tank[] GetTanks();
    public bool Drown(Tank tank);
}