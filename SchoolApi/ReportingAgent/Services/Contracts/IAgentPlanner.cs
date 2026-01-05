using GameAi.Api.ReportingAgent.Models;

namespace GameAi.Api.ReportingAgent.Services.Contracts
{
    public interface IAgentPlanner
    {
        Task<AgentPlan> CreatePlanAsync(string userMessage);
    }

}
