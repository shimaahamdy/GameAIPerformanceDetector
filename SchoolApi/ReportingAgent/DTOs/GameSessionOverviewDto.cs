namespace GameAi.Api.ReportingAgent.DTOs
{
    public class GameSessionOverviewDto
    {
        public string SessionId { get; set; } = null!;
        public List<NpcSessionSummaryDto> NpcSummaries { get; set; } = new();
        public List<string> PlayerIds { get; set; } = new();
        public int TotalTurns { get; set; }
    }

    public class NpcSessionSummaryDto
    {
        public string NpcId { get; set; } = null!;
        public string OverallTone { get; set; } = "neutral";
        public int Fairness { get; set; } = 5;
        public bool Escalation { get; set; } = false;
        public int TotalTurns { get; set; }
    }

}
