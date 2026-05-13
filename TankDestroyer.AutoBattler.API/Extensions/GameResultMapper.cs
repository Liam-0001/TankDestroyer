using TankDestroyer.AutoBattler.API.Dto;
using TankDestroyer.AutoBattler.Objects;

namespace TankDestroyer.AutoBattler.API.Extensions;

public static class GameResultMapper
{
    public static IEnumerable<BattleResultDto> ToDto(GameResult result)
    {
        var survivors = result.Bots.Where(t => !t.Destroyed).ToList();

        return result.BotInfo.Select(bot =>
        {
            var tank = result.Bots.FirstOrDefault(t => t.OwnerId == bot.OwnerId);
            var tankDead = tank?.Destroyed ?? true;

            int wins = 0, losses = 0, stalemates = 0, crashes = 0;

            if (result.HasCrashed)
            {
                crashes++;
            }
            else if (result.Bots.All(b => b.Destroyed) || result.IsStalemate && tank != null && !tankDead)
            {
                stalemates++;
            }
            else if (survivors.Count == 1 && survivors[0].OwnerId == bot.OwnerId)
            {
                wins++;
            }
            else
            {
                losses++;
            }

            return new BattleResultDto
            {
                Creator = bot.Creator,
                MapName = result.MapName,
                BotName = bot.Name,
                Wins = wins,
                Losses = losses,
                Stalemates = stalemates,
                Crashes = crashes
            };
        });
    }
}