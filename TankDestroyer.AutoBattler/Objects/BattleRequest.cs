namespace TankDestroyer.AutoBattler.Objects;

public class BattleRequest
{
    public int Id { get; set; }
    public DateTime RequestedAt { get; set; }
    public List<Game> Games { get; set; } = [];
    public int MaxTurns { get; set; }
}