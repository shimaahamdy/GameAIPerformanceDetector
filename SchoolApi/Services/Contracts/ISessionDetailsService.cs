using GameAi.Api.DTOs;

namespace GameAi.Api.Services.Contracts
{
    public interface ISessionDetailsService
    {
        Task<SessionDetailsDto?> GetSessionDetailsAsync(string sessionId, string npcId);
    }

}
