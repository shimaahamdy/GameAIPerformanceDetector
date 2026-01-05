using GameAi.Api.DTOs;
using GameAi.Api.Services;
using GameAi.Api.Services.Contracts;
using GameAI.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIJudgeController : ControllerBase
    {
        private readonly GameAIContext _dbContext;
        private readonly IJudgeService _judgeService;

        public AIJudgeController(GameAIContext dbContext, IJudgeService judgeService)
        {
            _dbContext = dbContext;
            _judgeService = judgeService;
        }



        /// <summary>
        /// Evaluate conversation for a session/player/NPC
        /// Fetches conversation from DB, sends to Judge, saves result, returns JudgeResult
        /// </summary>
        // POST /api/judge/run
        [HttpPost("run")]
        public async Task<ActionResult<List<JudgeOutputDto>>> RunJudgeForSession([FromBody] SessionEndDto input)
        {
            // 1️⃣ Load all NPCs in this session
            var npcPlayerPairs = await _dbContext.AiConversations
                .Where(c => c.SessionId == input.SessionId)
                .Select(c => new { c.NpcId, c.PlayerId })
                .Distinct()
                .ToListAsync();

            if (!npcPlayerPairs.Any())
                return BadRequest("No NPCs found for this session.");

            var results = new List<JudgeOutputDto>();

            // 2️⃣ Run judge per NPC
            foreach (var pair in npcPlayerPairs)
            {
                var judgeInput = new JudgeInputDto
                {
                    SessionId = input.SessionId,
                    PlayerId = pair.PlayerId,
                    NpcId = pair.NpcId
                    // Conversation and rules are loaded inside service
                };

                var judgeResult = await _judgeService.JudgeConversationAsync(judgeInput);

                results.Add(judgeResult);
            }

            return Ok(results);
        }
    }
}