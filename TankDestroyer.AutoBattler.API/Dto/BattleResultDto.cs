namespace TankDestroyer.AutoBattler.API.Dto;

public class BattleResultDto
{
    public required string Creator { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int Stalemates { get; init; }
    public int Crashes { get; init; }
}