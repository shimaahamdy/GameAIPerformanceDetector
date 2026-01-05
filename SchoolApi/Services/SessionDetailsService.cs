using GameAi.Api.DTOs;
using GameAi.Api.Services.Contracts;
using GameAI.Context;

namespace GameAi.Api.Services
{
    public class SessionDetailsService : ISessionDetailsService
    {
        private readonly GameAIContext _db;

        public SessionDetailsService(GameAIContext db)
        {
            _db = db;
        }

        public async Task<SessionDetailsDto?> GetSessionDetailsAsync(string sessionId, string npcId)
        {
            // 1️⃣ Load judge result for this session + NPC
            var judge = await _db.JudgeResults
                .Where(j => j.SessionId == sessionId && j.NpcId == npcId)
                .FirstOrDefaultAsync();

            if (judge == null)
                return null; // Or throw 404

            // 2️⃣ Load all conversations for this session + NPC
            var conversations = await _db.AiConversations
                .Where(c => c.SessionId == sessionId && c.NpcId == npcId)
                .OrderBy(c => c.Timestamp)
                .Select(c => new SessionConversationTurnDto
                {
                    PlayerMessage = c.PlayerMessage,
                    AiResponse = c.AiResponse,
                    Timestamp = c.Timestamp
                })
                .ToListAsync();

            // 3️⃣ Map to DTO
            return new SessionDetailsDto
            {
                SessionId = sessionId,
                PlayerId = judge.PlayerId,
                NpcId = npcId,
                Judge = new JudgeOutputDto
                {
                    OverallTone = judge.OverallTone,
                    FairnessScore = judge.FairnessScore,
                    InCharacter = judge.InCharacter,
                    EscalationTooFast = judge.EscalationTooFast,
                    Summary = judge.Summary
                },
                Conversation = conversations
            };
        }
    }

}
