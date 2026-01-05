using GameAi.Api.DTOs;
using GameAi.Api.Services;
using GameAi.Api.Services.Contracts;
using GameAI.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAi.Api.Controllers
{
    [ApiController]
    [Route("api/npcs")]
    public class NpcsController : ControllerBase
    {
        private readonly INpcAnalyticsService _analytics;
        private readonly INpcSessionService _npcSessionService;
        private readonly GameAIContext _db;
        public NpcsController(
            INpcAnalyticsService analytics,
            INpcSessionService npcSessionService,
            GameAIContext db)
        {
            _analytics = analytics;
            _npcSessionService = npcSessionService;
            _db = db;
        }


        [HttpGet("{npcId}/overview")]
        public async Task<ActionResult<NpcOverviewDto>> GetOverview(string npcId)
        {
            var result = await _analytics.GetNpcOverviewAsync(npcId);
            return Ok(result);
        }


        [HttpGet("sessions/summary/{sessionId}")]
        public async Task<List<SessionNpcSummaryDto>> GetSessionNpcSummariesAsync(string sessionId)
        {
            return await _db.JudgeResults
                .Where(j => j.SessionId == sessionId)
                .Select(j => new SessionNpcSummaryDto
                {
                    NpcId = j.NpcId,
                    OverallTone = j.OverallTone,
                    InCharacter = j.InCharacter,
                    FairnessScore = j.FairnessScore,
                    EscalationTooFast = j.EscalationTooFast,
                    Summary = j.Summary
                })
                .ToListAsync();
        }


        [HttpGet("sessions/{sessionId}/{npcId}")]
        public async Task<List<ConversationTurnDto>> GetConversationAsync(
    string sessionId,
    string npcId)
        {
            return await _db.AiConversations
                .Where(c =>
                    c.SessionId == sessionId &&
                    c.NpcId == npcId)
                .OrderBy(c => c.Timestamp)
                .SelectMany(c => new[]
                {
            c.PlayerMessage != null
                ? new ConversationTurnDto
                {
                    Timestamp = c.Timestamp,
                    Speaker = "player",
                    Message = c.PlayerMessage
                }
                : null,

            c.AiResponse != null
                ? new ConversationTurnDto
                {
                    Timestamp = c.Timestamp,
                    Speaker = "npc",
                    Message = c.AiResponse
                }
                : null
                })
                .Where(x => x != null)
                .ToListAsync();
        }



    }

}
