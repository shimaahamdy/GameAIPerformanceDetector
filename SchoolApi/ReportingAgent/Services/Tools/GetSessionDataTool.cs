using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Models;
using GameAi.Api.ReportingAgent.Models.ReAct;
using GameAi.Api.ReportingAgent.Services.Contracts;
using GameAi.Api.ReportingAgent.Services;

namespace GameAi.Api.ReportingAgent.Services.Tools
{
    /// <summary>
    /// Tool for retrieving session data and metrics
    /// </summary>
    public class GetSessionDataTool : IReActTool
    {
        private readonly IGameSessionService _sessionService;

        public string Name => "get_session_data";
        public string Description => "Retrieves game session data including metrics, NPC summaries, and statistics. Requires 'sessionId' parameter.";

        public GetSessionDataTool(IGameSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<ReActObservation> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                if (!parameters.TryGetValue("sessionId", out var sessionIdObj) || sessionIdObj == null)
                {
                    return new ReActObservation
                    {
                        ToolName = Name,
                        Success = false,
                        ErrorMessage = "sessionId parameter is required"
                    };
                }

                var sessionId = sessionIdObj.ToString()!;
                var overview = await _sessionService.GetSessionOverviewAsync(sessionId);
                var npcs = await _sessionService.GetSessionNpcsAsync(sessionId);

                var result = new AgentDataResult
                {
                    Metrics = new Dictionary<string, object>
                    {
                        ["SessionId"] = sessionId,
                        ["TotalNpcs"] = overview.NpcSummaries?.Count ?? 0,
                        ["TotalMessages"] = overview.TotalTurns,
                        ["AvgFairness"] = overview.NpcSummaries?.Any() == true
                            ? Math.Round(overview.NpcSummaries.Average(n => n.Fairness), 2)
                            : 5.0,
                        ["EscalationRate"] = overview.NpcSummaries?.Any() == true
                            ? Math.Round(overview.NpcSummaries.Count(n => n.Escalation) / (double)overview.NpcSummaries.Count, 2)
                            : 0.0
                    }
                };

                return new ReActObservation
                {
                    ToolName = Name,
                    Success = true,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return new ReActObservation
                {
                    ToolName = Name,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}

