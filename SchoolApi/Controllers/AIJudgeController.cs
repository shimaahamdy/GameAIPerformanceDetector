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

        public AIJudgeController(GameAIContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Evaluate conversation for a session/player/NPC
        /// Fetches conversation from DB, sends to Judge, saves result, returns JudgeResult
        /// </summary>
        [HttpPost("judge")]
        public async Task<IActionResult> JudgeConversation(
            [FromQuery] string sessionId,
            [FromQuery] string playerId,
            [FromQuery] string npcId,
            [FromServices] IJudgeService _judgeService)
        {
            // 1️⃣ Fetch conversation from DB
            var messages = await _dbContext.AiConversations
                .Where(x => x.SessionId == sessionId && x.PlayerId == playerId && x.NpcId == npcId)
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            if (!messages.Any())
                return NotFound("No conversation found for this session/player/NPC.");

            // 2️⃣ Map to DTO
            var conversationDto = messages
                .SelectMany(x => new List<ConversationTurnDto>
                {
                new() { Speaker = "player", Message = x.PlayerMessage },
                new() { Speaker = "npc", Message = x.AiResponse }
                })
                .ToList();

            // 3️⃣ Prepare Judge input
            var judgeInput = new JudgeInputDto
            {
                SessionId = sessionId,
                PlayerId = playerId,
                NpcId = npcId,
                NpcRole = "NPC role description here", // optional: fetch from NPC table if exists
                Conversation = conversationDto
            };

            // 4️⃣ Call Judge service (mock or real AI)
            var judgeResult = await _judgeService.JudgeConversationAsync(judgeInput);

            return Ok(new
            {
                sessionId,
                playerId,
                npcId,
                conversation = conversationDto,
                judgeResult
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? npcId, [FromQuery] string? playerId, [FromQuery] int limit = 50)
        {
            var query = _dbContext.JudgeResults.AsQueryable();

            if (!string.IsNullOrEmpty(npcId))
                query = query.Where(x => x.NpcId == npcId);

            if (!string.IsNullOrEmpty(playerId))
                query = query.Where(x => x.PlayerId == playerId);

            var results = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    x.SessionId,
                    x.PlayerId,
                    x.NpcId,
                    x.OverallTone,
                    x.InCharacter,
                    x.FairnessScore,
                    x.EscalationTooFast,
                    x.Summary,
                    x.CreatedAt
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET single judge result by Id
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _dbContext.JudgeResults
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.SessionId,
                    x.PlayerId,
                    x.NpcId,
                    x.OverallTone,
                    x.InCharacter,
                    x.FairnessScore,
                    x.EscalationTooFast,
                    x.Summary,
                    x.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
