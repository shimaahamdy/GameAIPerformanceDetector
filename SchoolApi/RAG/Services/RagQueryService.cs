using Data.Models;
using GameAi.Api.RAG.Helpers;
using GameAi.Api.RAG.Models;
using GameAi.Api.RAG.Services.Contracts;
using System.Text;

namespace GameAi.Api.RAG.Services
{
    public class RagQueryService : IRagQueryService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly VectorStore _vectorStore;

        public RagQueryService(
            IEmbeddingService embeddingService,
            VectorStore vectorStore)
        {
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
        }

        public async Task<RagContextResult> QueryAsync(
            string npcId,
            string sessionId,
            List<AiConversation> currentSession,
            int topK = 5)
        {
            // 1️⃣ Build query text from current session
            var queryText = BuildConversationText(currentSession);

            // 2️⃣ Create embedding
            var queryEmbedding = await _embeddingService.CreateEmbeddingAsync(queryText);

            // 3️⃣ Filter vectors for same NPC (VERY IMPORTANT)
            var candidates = _vectorStore
                .GetAll()
                .Where(v => v.NpcId == npcId)
                .Select(v => new
                {
                    Vector = v,
                    Score = VectorMath.CosineSimilarity(queryEmbedding, v.Embedding)
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            // 4️⃣ Split results
            var result = new RagContextResult();

            foreach (var item in candidates)
            {
                if (item.Vector.SourceType == "Rule")
                    result.Rules.Add(item.Vector.Content);

                if (item.Vector.SourceType == "Conversation")
                    result.SimilarConversations.Add(item.Vector.Content);
            }

            return result;
        }

        private static string BuildConversationText(List<AiConversation> session)
        {
            var sb = new StringBuilder();

            foreach (var turn in session.OrderBy(x => x.Timestamp))
            {
                sb.AppendLine($"Player: {turn.PlayerMessage}");
                sb.AppendLine($"NPC: {turn.AiResponse}");
            }

            return sb.ToString();
        }
    }


}
