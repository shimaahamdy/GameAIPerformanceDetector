using GameAi.Api.Models;
using GameAi.Api.RAG;
using GameAi.Api.RAG.Services.Contracts;
using GameAi.Api.ReportingAgent.ChatRag;
using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Services.Contracts;
using GameAI.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameAi.Api.ReportingAgent.Controllers
{
    [ApiController]
    [Route("api/reporting-agent")]
    [Authorize]
    public class ChartsAgentController : ControllerBase
    {
        private readonly IChartsAgentService _agent;
        private readonly GameAIContext _db;
        private readonly IEmbeddingService _embeddingService;
        private readonly DeveloperAgentService _devAgent;
        private readonly DevInMemoryVectorStore _vectorStore;

        public ChartsAgentController(IChartsAgentService agent, GameAIContext db, IEmbeddingService embeddingService, DeveloperAgentService devAgent, DevInMemoryVectorStore vectorStore)
        {
            _agent = agent;
            _db = db;
            _embeddingService = embeddingService;
            _devAgent = devAgent;
            _vectorStore = vectorStore;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChartsAgentChatRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body cannot be null." });

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message cannot be null or empty." });

            try
            {
                var developerId = User.Identity.Name;
                // 1️ Save developer request
                var devMsg = new DeveloperMessage
                {
                    DeveloperId = developerId,
                    Role = "developer",
                    Content = request.Message
                };
                _db.DeveloperMessages.Add(devMsg);
                await _db.SaveChangesAsync();

                // 2️⃣ Embed new request
                var embedding = await _embeddingService.CreateEmbeddingAsync(request.Message);
                _vectorStore.Add(new DevVectorEntry
                {
                    Id = devMsg.Id.ToString(),
                    DeveloperId = developerId,
                    Content = request.Message,
                    Embedding = embedding
                });

                // 3️⃣ Search top-K relevant past messages
                var relevant = _vectorStore.Search(embedding, topK: 5)
                    .Where(v => v.DeveloperId == developerId)
                    .Select(v => v.Content)
                    .ToList();

                // 4️⃣ Build prompt with relevant context
                var context = string.Join("\n\n", relevant);
                var finalPrompt = $"Developer request:\n{request}\n\nRelevant past messages:\n{context}";
                var response = await _agent.HandleAsync(request.Message);

                // 6️⃣ Save AI response
                var aiMsg = new DeveloperMessage
                {
                    DeveloperId = developerId,
                    Role = "agent",
                    Content = response.ToString()
                };
                _db.DeveloperMessages.Add(aiMsg);
                await _db.SaveChangesAsync();

                // 7️⃣ Embed AI response into vector store
                var aiEmbedding = await _embeddingService.CreateEmbeddingAsync(response.Text);
                _vectorStore.Add(new DevVectorEntry
                {
                    Id = aiMsg.Id.ToString(),
                    DeveloperId = developerId,
                    Content = response.Text,
                    Embedding = aiEmbedding
                });

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { error = "Processing failed.", details = ex.Message });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }

}
