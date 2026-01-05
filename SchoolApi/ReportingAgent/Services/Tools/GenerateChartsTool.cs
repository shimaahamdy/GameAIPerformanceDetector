using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Models;
using GameAi.Api.ReportingAgent.Models.ReAct;
using GameAi.Api.ReportingAgent.Services.Contracts;
using GameAi.Api.ReportingAgent.Services;

namespace GameAi.Api.ReportingAgent.Services.Tools
{
    /// <summary>
    /// Tool for generating charts from session data
    /// </summary>
    public class GenerateChartsTool : IReActTool
    {
        private readonly IGameSessionService _sessionService;

        public string Name => "generate_charts";
        public string Description => "Generates visualization charts (pie, bar) from session data. Requires 'sessionId' parameter. Returns chart data for tone distribution, fairness, and escalation.";

        public GenerateChartsTool(IGameSessionService sessionService)
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
                var npcs = await _sessionService.GetSessionNpcsAsync(sessionId);

                if (!npcs.Any())
                {
                    return new ReActObservation
                    {
                        ToolName = Name,
                        Success = false,
                        ErrorMessage = "No NPC data found for this session"
                    };
                }

                var charts = new List<ChartDto>
                {
                    BuildToneDistributionChart(npcs),
                    BuildFairnessChart(npcs),
                    BuildEscalationChart(npcs)
                };

                return new ReActObservation
                {
                    ToolName = Name,
                    Success = true,
                    Result = charts
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

        private ChartDto BuildToneDistributionChart(List<DTOs.NpcSessionSummaryDto> npcs)
        {
            var groups = npcs
                .GroupBy(n => n.OverallTone ?? "neutral")
                .Select(g => new { Tone = g.Key, Count = g.Count() })
                .ToList();

            return new ChartDto
            {
                Type = "pie",
                Title = "NPC Tone Distribution",
                Labels = groups.Select(g => g.Tone).ToList(),
                Values = groups.Select(g => (double)g.Count).ToList()
            };
        }

        private ChartDto BuildFairnessChart(List<DTOs.NpcSessionSummaryDto> npcs)
        {
            return new ChartDto
            {
                Type = "bar",
                Title = "NPC Average Fairness",
                Labels = npcs.Select(n => n.NpcId).ToList(),
                Values = npcs.Select(n => (double)n.Fairness).ToList()
            };
        }

        private ChartDto BuildEscalationChart(List<DTOs.NpcSessionSummaryDto> npcs)
        {
            return new ChartDto
            {
                Type = "bar",
                Title = "NPC Escalation (1 = escalated)",
                Labels = npcs.Select(n => n.NpcId).ToList(),
                Values = npcs.Select(n => n.Escalation ? 1.0 : 0.0).ToList()
            };
        }
    }
}

