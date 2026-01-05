using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Models;

namespace GameAi.Api.ReportingAgent.Services.Contracts
{
    public interface IAgentDataService
    {
        Task<Models.AgentDataResult> ExecutePlanAsync(AgentPlan plan);
    }

}
