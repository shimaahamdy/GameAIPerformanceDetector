namespace GameAi.Api.ReportingAgent.Models
{

    public class AgentPlan
    {
        public string Intent { get; set; } = "";  // session_report, npc_analysis, comparison
        public string? SessionId { get; set; }
        public string? NpcId { get; set; }
        public bool NeedsCharts { get; set; }
        public bool NeedsPdf { get; set; }

    }

}
