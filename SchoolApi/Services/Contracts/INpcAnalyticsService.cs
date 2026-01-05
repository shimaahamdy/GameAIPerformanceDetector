using GameAi.Api.DTOs;

namespace GameAi.Api.Services.Contracts
{
    public interface INpcAnalyticsService
    {
        Task<NpcOverviewDto> GetNpcOverviewAsync(string npcId);
        Task<List<NpcOverviewDto>> GetAllNpcSummariesAsync();
    }

}
