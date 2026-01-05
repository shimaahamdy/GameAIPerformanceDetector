using System.Linq;
using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Models;
using GameAi.Api.ReportingAgent.Services.Contracts;
using GameAi.Api.Services.Contracts;

namespace GameAi.Api.ReportingAgent.Services
{
    public class AgentDataService : IAgentDataService
    {
        private readonly IGameSessionService _sessionService;
        private readonly IJudgeService _judgeService;

        public AgentDataService(IGameSessionService sessionService,
                                IJudgeService judgeService)
        {
            _sessionService = sessionService;
            _judgeService = judgeService;
        }

        public async Task<AgentDataResult> ExecutePlanAsync(AgentPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            var result = new AgentDataResult();

            switch (plan.Intent)
            {
                case AgentIntent.SessionReport:
                    // Validate SessionId is provided
                    if (string.IsNullOrWhiteSpace(plan.SessionId))
                        throw new ArgumentException("SessionId is required for session_report intent.", nameof(plan));

                    // 1️ Get session overview (use existing DTO shape)
                    var sessionOverview = await _sessionService.GetSessionOverviewAsync(plan.SessionId);

                    result.Metrics["SessionId"] = plan.SessionId;
                    result.Metrics["TotalNpcs"] = sessionOverview.NpcSummaries?.Count ?? 0;
                    result.Metrics["TotalMessages"] = sessionOverview.TotalTurns;
                    // compute average fairness from the per-npc summaries (fallback to 5)
                    result.Metrics["AvgFairness"] = sessionOverview.NpcSummaries?.Any() == true
                        ? Math.Round(sessionOverview.NpcSummaries.Average(n => n.Fairness), 2)
                        : 5.0;
                    // compute escalation rate as fraction of NPCs with escalation
                    result.Metrics["EscalationRate"] = sessionOverview.NpcSummaries?.Any() == true
                        ? Math.Round(sessionOverview.NpcSummaries.Count(n => n.Escalation) / (double)sessionOverview.NpcSummaries.Count, 2)
                        : 0.0;

                    // 2️ Get NPC stats in session (use new/existing service method)
                    var npcs = await _sessionService.GetSessionNpcsAsync(plan.SessionId);

                    result.Metrics["NpcSummary"] = npcs;

                    // 3️⃣ Build chart-ready data if requested (adapted to available fields)
                    if (plan.NeedsCharts)
                    {
                        result.Charts.Add(BuildToneDistributionChart(npcs));
                        result.Charts.Add(BuildFairnessChart(npcs));
                        result.Charts.Add(BuildEscalationChart(npcs));
                    }

                    break;

                case AgentIntent.NpcAnalysis:
                    // This requires judge service to expose a GetNpcOverviewAsync method.
                    // Keep explicit so the caller knows to add that method to IJudgeService if desired.
                    throw new NotSupportedException("npc_analysis intent requires IJudgeService.GetNpcOverviewAsync to be implemented.");
                
                case AgentIntent.Comparison:
                    throw new NotSupportedException("comparison intent is not yet implemented.");
                
                default:
                    throw new NotSupportedException($"Intent '{plan.Intent}' is not supported.");
            }

            return result;
        }

        #region Chart Builders
        private ChartDto BuildToneDistributionChart(List<DTOs.NpcSessionSummaryDto> npcs)
        {
            if (npcs == null || !npcs.Any())
                return new ChartDto
                {
                    Type = "pie",
                    Title = "NPC Tone Distribution",
                    Labels = new List<string> { "No data" },
                    Values = new List<double> { 0 }
                };

            // Count OverallTone occurrences: "friendly", "neutral", "hostile" etc.
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
            if (npcs == null || !npcs.Any())
                return new ChartDto
                {
                    Type = "bar",
                    Title = "NPC Average Fairness",
                    Labels = new List<string>(),
                    Values = new List<double>()
                };

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
            if (npcs == null || !npcs.Any())
                return new ChartDto
                {
                    Type = "bar",
                    Title = "NPC Escalation (1 = escalated)",
                    Labels = new List<string>(),
                    Values = new List<double>()
                };

            return new ChartDto
            {
                Type = "bar",
                Title = "NPC Escalation (1 = escalated)",
                Labels = npcs.Select(n => n.NpcId).ToList(),
                Values = npcs.Select(n => n.Escalation ? 1.0 : 0.0).ToList()
            };
        }
        #endregion
    }

}
