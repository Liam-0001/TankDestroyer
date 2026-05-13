namespace TankDestroyer.AutoBattler.API.Dto;

public record BattleResultDto
{
    public required string BotName { get; init; }
    public required string Creator { get; init; }
    public required string MapName { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int Stalemates { get; init; }
    public int Crashes { get; init; }
}