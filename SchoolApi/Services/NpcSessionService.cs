using GameAi.Api.DTOs;
using GameAi.Api.Services.Contracts;
using GameAI.Context;
using Microsoft.EntityFrameworkCore;

namespace GameAi.Api.Services
{
    public class NpcSessionService : INpcSessionService
    {
        private readonly GameAIContext _db;

        public NpcSessionService(GameAIContext db)
        {
            _db = db;
        }

        public async Task<List<NpcSessionSummaryDto>> GetNpcSessionsAsync(string npcId)
        {
            return await _db.JudgeResults
                .Where(j => j.NpcId == npcId)
                .OrderByDescending(j => j.CreatedAt)
                .Select(j => new NpcSessionSummaryDto
                {
                    SessionId = j.SessionId,
                    PlayerId = j.PlayerId,
                    Tone = j.OverallTone,
                    Fairness = j.FairnessScore,
                    InCharacter = j.InCharacter,
                    EscalationTooFast = j.EscalationTooFast,
                    Summary = j.Summary,
                    CreatedAt = j.CreatedAt
                })
                .ToListAsync();
        }


    }

}
