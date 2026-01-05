using GameAi.Api.ReportingAgent.DTOs;

namespace GameAi.Api.ReportingAgent.Services.Contracts
{
    public interface IChartsAgentService
    {
        Task<ChartsAgentChatResponse> HandleAsync(string userMessage);
    }

}
