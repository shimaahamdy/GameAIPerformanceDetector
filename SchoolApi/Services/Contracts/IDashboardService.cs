using GameAi.Api.DTOs;

namespace GameAi.Api.Services.Contracts
{
    public interface IDashboardService
    {
        Task<List<DashboardConversationDto>> GetSessionsAsync();
        Task<DashboardConversationDto?> GetSessionByIdAsync(string sessionId, string playerId, string npcId);
    }

}
