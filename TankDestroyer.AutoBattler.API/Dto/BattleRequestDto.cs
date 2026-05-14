namespace TankDestroyer.AutoBattler.API.Dto;

public class BattleRequestDto
{
    public string? MapName { get; set; }
    public int MaxTurns { get; set; } = 200;
}
