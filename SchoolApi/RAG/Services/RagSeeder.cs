using GameAi.Api.RAG.Services.Contracts;
using GameAI.Context;

namespace GameAi.Api.RAG.Services
{
    public class RagSeeder : IRagSeeder
    {
        private readonly GameAiDbContext _db;
        private readonly IEmbeddingService _embeddingService;
        private readonly VectorStore _vectorStore;

        public RagSeeder(
            GameAiDbContext db,
            IEmbeddingService embeddingService,
            VectorStore vectorStore)
        {
            _db = db;
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
        }

        public async Task SeedAsync()
        {
            await SeedNpcRules();
            await SeedPastConversations();
        }

        private async Task SeedNpcRules()
        {
            var rules = await _db.NpcRules.ToListAsync();

            foreach (var rule in rules)
            {
                var embedding = await _embeddingService.CreateEmbeddingAsync(rule.RuleText);

                _vectorStore.Add(new RagVector
                {
                    Id = $"rule-{rule.Id}",
                    Embedding = embedding,
                    Content = rule.RuleText,
                    SourceType = "Rule",
                    NpcId = rule.NpcId
                });
            }
        }

        private async Task SeedPastConversations()
        {
            var conversations = await _db.AiConversations.ToListAsync();

            foreach (var c in conversations)
            {
                var text =
    $"""
Player: {c.PlayerMessage}
NPC: {c.AiResponse}
""";

                var embedding = await _embeddingService.CreateEmbeddingAsync(text);

                _vectorStore.Add(new RagVector
                {
                    Id = $"conv-{c.Id}",
                    Embedding = embedding,
                    Content = text,
                    SourceType = "Conversation",
                    NpcId = c.NpcId,
                    SessionId = c.SessionId
                });
            }
        }
    }

}
