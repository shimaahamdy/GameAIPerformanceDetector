using Data.Models;
using GameAi.Api.RAG.Models;

namespace GameAi.Api.RAG.Services.Contracts
{
    public interface IRagQueryService
    {
        Task<RagContextResult> QueryAsync(
            string npcId,
            string sessionId,
            List<AiConversation> currentSession,
            int topK = 5);
    }

}
