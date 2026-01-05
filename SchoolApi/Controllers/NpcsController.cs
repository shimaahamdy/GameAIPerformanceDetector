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
            var result = await _db.JudgeResults
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

            return result;
        }




        /// <summary>
        /// Get overview summaries for all NPCs based on judge results
        /// </summary>
        [HttpGet("overview/all")]
        public async Task<ActionResult<List<NpcOverviewDto>>> GetAllNpcSummaries()
        {
            var summaries = await _analytics.GetAllNpcSummariesAsync();
            return Ok(summaries);
        }



    }

}
