using GameAi.Api.DTOs;
using GameAi.Api.Services.Contracts;
using GameAI.Context;
using Microsoft.EntityFrameworkCore;

namespace GameAi.Api.Services
{
    public class NpcAnalyticsService : INpcAnalyticsService
    {
        private readonly GameAIContext _db;

        public NpcAnalyticsService(GameAIContext db)
        {
            _db = db;
        }

        public async Task<NpcOverviewDto> GetNpcOverviewAsync(string npcId)
        {
            var judges = await _db.JudgeResults
                .Where(j => j.NpcId == npcId)
                .ToListAsync();

            if (!judges.Any())
            {
                return new NpcOverviewDto
                {
                    NpcId = npcId,
                    TotalSessions = 0
                };
            }

            var total = judges.Count;

            return new NpcOverviewDto
            {
                NpcId = npcId,
                TotalSessions = total,

                AverageFairness = Math.Round(
                    judges.Average(j => j.FairnessScore), 2),

                ToneDistribution = new ToneDistributionDto
                {
                    Friendly = judges.Count(j => j.OverallTone?.ToLower() == "friendly"),
                    Neutral = judges.Count(j => j.OverallTone?.ToLower() == "neutral"),
                    Hostile = judges.Count(j => j.OverallTone?.ToLower() == "hostile")
                },

                InCharacterRate = Math.Round(
                    judges.Count(j => j.InCharacter) / (double)total, 2),

                EscalationRate = Math.Round(
                    judges.Count(j => j.EscalationTooFast) / (double)total, 2)
            };
        }

    }

}
