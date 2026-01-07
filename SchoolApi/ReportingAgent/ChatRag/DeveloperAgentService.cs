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
                var embedding = await _embeddingService.CreateEmbeddingAsync(msg.Content);
                _vectorStore.Add(new DevVectorEntry
                {
                    Id = msg.Id.ToString(),
                    Content = msg.Content,
                    DeveloperId = developerId,
                    Embedding = embedding
                });
            }
        }

        // Handle new developer request
        public async Task<string> HandleDeveloperRequestAsync(string developerId, string request)
        {
            // 1️ Save developer request
            var devMsg = new DeveloperMessage
            {
                DeveloperId = developerId,
                Role = "developer",
                Content = request
            };
            _db.DeveloperMessages.Add(devMsg);
            await _db.SaveChangesAsync();

            // 2️⃣ Embed new request
            var embedding = await _embeddingService.CreateEmbeddingAsync(request);
            _vectorStore.Add(new DevVectorEntry
            {
                Id = devMsg.Id.ToString(),
                DeveloperId = developerId,
                Content = request,
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

            // 5️⃣ Call ReAct Agent
            var response = await _reactAgent.HandleAsync(finalPrompt);

            // 6️⃣ Save AI response
            var aiMsg = new DeveloperMessage
            {
                DeveloperId = developerId,
                Role = "agent",
                Content = response.Text
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

            return response.Text;
        }
    }

}
