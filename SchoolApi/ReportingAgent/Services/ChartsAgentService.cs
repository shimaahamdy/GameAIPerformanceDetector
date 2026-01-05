using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Services.Contracts;

namespace GameAi.Api.ReportingAgent.Services
{
    public class ChartsAgentService : IChartsAgentService
    {
        private readonly IAgentPlanner _planner;
        private readonly IAgentDataService _data;
        private readonly IAgentAiService _ai;

        public ChartsAgentService(
            IAgentPlanner planner,
            IAgentDataService data,
            IAgentAiService ai)
        {
            _planner = planner;
            _data = data;
            _ai = ai;
        }

        public async Task<ChartsAgentChatResponse> HandleAsync(string userMessage)
        {
            // 1️ Ask AI: what does the user want?
            var plan = await _planner.CreatePlanAsync(userMessage);

            // 2️ Execute plan using tools (C#)
            var analysisResult = await _data.ExecutePlanAsync(plan);

            // 3️ Ask AI to explain results
            return await _ai.GenerateResponseAsync(userMessage, plan, analysisResult);
        }
    }

}
