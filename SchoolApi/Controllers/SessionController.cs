using GameAi.Api.DTOs;
using GameAI.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly GameAIContext _db;

        public SessionController(GameAIContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Returns all session IDs with start & end times
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SessionListDto>>> GetAllSessions()
        {
            var sessions = await _db.AiConversations
                .AsNoTracking()
                .GroupBy(c => c.SessionId)
                .Select(g => new SessionListDto
                {
                    SessionId = g.Key,
                    CreatedAt = g.Min(x => x.Timestamp),
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("session-conversation/{sessionId}/{npcId}")]
        public async Task<List<ConversationTurnDto>> GetConversationAsync(string sessionId, string npcId)
        {
            // 1️⃣ Load all relevant conversation rows from the DB
            var conversationRows = await _db.AiConversations
                .Where(c => c.SessionId == sessionId && c.NpcId == npcId)
                .OrderBy(c => c.Timestamp)
                .ToListAsync();

            // 2️⃣ Project to ConversationTurnDto in memory
            var conversation = new List<ConversationTurnDto>();

            foreach (var c in conversationRows)
            {
                if (!string.IsNullOrEmpty(c.PlayerMessage))
                {
                    conversation.Add(new ConversationTurnDto
                    {
                        Timestamp = c.Timestamp,
                        Speaker = "player",
                        Message = c.PlayerMessage
                    });
                }

                if (!string.IsNullOrEmpty(c.AiResponse))
                {
                    conversation.Add(new ConversationTurnDto
                    {
                        Timestamp = c.Timestamp,
                        Speaker = "npc",
                        Message = c.AiResponse
                    });
                }
            }

            return conversation;
        }
    }
}

