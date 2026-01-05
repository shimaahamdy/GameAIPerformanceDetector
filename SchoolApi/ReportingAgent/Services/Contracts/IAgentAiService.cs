using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Models;

namespace GameAi.Api.ReportingAgent.Services.Contracts
{
    public interface IAgentAiService
    {
        Task<ChartsAgentChatResponse> GenerateResponseAsync(string userMessage, AgentPlan plan, AgentDataResult data);
    }

}
