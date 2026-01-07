using Data.Models;
using GameAi.Api.DTOs;
using GameAI.Context;
using GameAI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GameConversationsController : ControllerBase
    {
        public GameAIContext GameAIContext { get; set; }

        public GameConversationsController(GameAIContext _gameAIContext)
        {
            GameAIContext = _gameAIContext;
        }

        [HttpPost]
        public async Task<IActionResult> SaveConversation([FromBody] AiConversationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = new AiConversation
            {
                Id = Guid.NewGuid(),
                SessionId = dto.SessionId,
                PlayerId = dto.PlayerId,
                NpcId = dto.NpcId,
                PlayerMessage = dto.PlayerMessage,
                AiResponse = dto.AiResponse,
                Timestamp = dto.Timestamp,
                CreatedAt = DateTime.UtcNow
            };

            GameAIContext.AiConversations.Add(entity);
            await GameAIContext.SaveChangesAsync();

            return Ok(new { entity.Id });
        }


        [HttpPost("SaveConversations")]
        public async Task<IActionResult> SaveConversations([FromBody] List<AiConversationDto> dtos)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entities = new List<AiConversation>();

            foreach (var dto in dtos)
            {
                // Validate each DTO individually
                if (string.IsNullOrWhiteSpace(dto.SessionId) ||
                    string.IsNullOrWhiteSpace(dto.PlayerId) ||
                    string.IsNullOrWhiteSpace(dto.NpcId) ||
                    string.IsNullOrWhiteSpace(dto.PlayerMessage) ||
                    string.IsNullOrWhiteSpace(dto.AiResponse))
                {
                    return BadRequest($"Invalid conversation entry for SessionId: {dto.SessionId}, PlayerId: {dto.PlayerId}");
                }

                entities.Add(new AiConversation
                {
                    Id = Guid.NewGuid(),
                    SessionId = dto.SessionId,
                    PlayerId = dto.PlayerId,
                    NpcId = dto.NpcId,
                    PlayerMessage = dto.PlayerMessage,
                    AiResponse = dto.AiResponse,
                    Timestamp = dto.Timestamp,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Add all entities in one go
            GameAIContext.AiConversations.AddRange(entities);
            await GameAIContext.SaveChangesAsync();

            // Return list of inserted IDs
            var ids = entities.Select(e => e.Id).ToList();
            return Ok(new { ids });
        }


        [HttpGet]
        public async Task<IActionResult> GetConversations([FromQuery] string? npcId, [FromQuery] string? playerId, [FromQuery] int limit = 50)
        {
            var query = GameAIContext.AiConversations.AsQueryable();

            if (!string.IsNullOrEmpty(npcId))
                query = query.Where(x => x.NpcId == npcId);

            if (!string.IsNullOrEmpty(playerId))
                query = query.Where(x => x.PlayerId == playerId);

            var result = await query
                .OrderByDescending(x => x.Timestamp)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    x.SessionId,
                    x.PlayerId,
                    x.NpcId,
                    x.PlayerMessage,
                    x.AiResponse,
                    x.Timestamp
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetConversationById(Guid id)
        {
            var conversation = await GameAIContext.AiConversations
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.SessionId,
                    x.PlayerId,
                    x.NpcId,
                    x.PlayerMessage,
                    x.AiResponse,
                    x.Timestamp
                })
                .FirstOrDefaultAsync();

            if (conversation == null)
                return NotFound();

            return Ok(conversation);
        }

        [HttpGet("thread")]
        public async Task<IActionResult> GetConversationThread([FromQuery] string sessionId, [FromQuery] string playerId, [FromQuery] string npcId)
        {
            var messages = await GameAIContext.AiConversations
                .Where(x =>
                    x.SessionId == sessionId &&
                    x.PlayerId == playerId &&
                    x.NpcId == npcId)
                .OrderBy(x => x.Timestamp)
                .Select(x => new
                {
                    speaker = "player",
                    message = x.PlayerMessage,
                    timestamp = x.Timestamp
                })
                .Concat(
                    GameAIContext.AiConversations
                        .Where(x =>
                            x.SessionId == sessionId &&
                            x.PlayerId == playerId &&
                            x.NpcId == npcId)
                        .Select(x => new
                        {
                            speaker = "npc",
                            message = x.AiResponse,
                            timestamp = x.Timestamp
                        })
                )
                .OrderBy(x => x.timestamp)
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("thread-with-judge")]
        public async Task<IActionResult> GetConversationWithJudge([FromQuery] string sessionId, [FromQuery] string playerId, [FromQuery] string npcId)
        {
            var messages = await GameAIContext.AiConversations
                .Where(x => x.SessionId == sessionId && x.PlayerId == playerId && x.NpcId == npcId)
                .OrderBy(x => x.Timestamp)
                .Select(x => new ConversationTurnDto
                {
                    Speaker = "player",
                    Message = x.PlayerMessage
                })
                .Concat(
                    GameAIContext.AiConversations
                        .Where(x => x.SessionId == sessionId && x.PlayerId == playerId && x.NpcId == npcId)
                        .Select(x => new ConversationTurnDto
                        {
                            Speaker = "npc",
                            Message = x.AiResponse
                        })
                )
                .OrderBy(x => x.Timestamp)
                .ToListAsync();

            // Find judge result
            var judge = await GameAIContext.JudgeResults
                .Where(x => x.SessionId == sessionId && x.PlayerId == playerId && x.NpcId == npcId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                sessionId,
                playerId,
                npcId,
                messages,
                JudgeId = judge?.Id ?? Guid.Empty
            });
        }




    }
}
