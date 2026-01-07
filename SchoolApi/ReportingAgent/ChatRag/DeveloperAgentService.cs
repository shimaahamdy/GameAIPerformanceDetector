using GameAi.Api.Models;
using GameAi.Api.RAG.Services.Contracts;
using GameAi.Api.ReportingAgent.Services;
using GameAi.Api.ReportingAgent.Services.Contracts;
using GameAI.Context;
using Microsoft.EntityFrameworkCore;

namespace GameAi.Api.ReportingAgent.ChatRag
{
    public class DeveloperAgentService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GameAIContext _db;
        private readonly DevInMemoryVectorStore _vectorStore;
        private readonly IEmbeddingService _embeddingService; // wraps OpenAI embeddings
        private readonly IChartsAgentService _reactAgent;

        public DeveloperAgentService(
            IHttpClientFactory httpClientFactory,
            GameAIContext db,
            DevInMemoryVectorStore vectorStore,
            IEmbeddingService embeddingService,
            IChartsAgentService reactAgent)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
            _vectorStore = vectorStore;
            _embeddingService = embeddingService;
            _reactAgent = reactAgent;
        }

        // Load old messages on startup
        public async Task InitializeDeveloperMemoryAsync(string developerId)
        {
            var messages = await _db.DeveloperMessages
                .Where(m => m.DeveloperId == developerId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            foreach (var msg in messages)
            {
                float[] embedding = null;
                if(msg.Role != "agent")
                    embedding = await _embeddingService.CreateEmbeddingAsync(msg.Content);
                else embedding = await _embeddingService.CreateEmbeddingAsync(msg.Summary);
                _vectorStore.Add(new DevVectorEntry
                {
                    Id = msg.Id.ToString(),
                    Content = msg.Summary,
                    DeveloperId = developerId,
                    Embedding = embedding
                });
            }
        }

    }

}
