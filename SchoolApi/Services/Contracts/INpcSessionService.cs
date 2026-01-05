using GameAi.Api.DTOs;

namespace GameAi.Api.Services.Contracts
{
    public interface INpcSessionService
    {
        Task<List<NpcSessionSummaryDto>> GetNpcSessionsAsync(string npcId);
    }

}
