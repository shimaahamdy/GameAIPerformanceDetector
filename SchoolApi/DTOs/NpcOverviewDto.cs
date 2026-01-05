namespace GameAi.Api.DTOs
{
    public class NpcOverviewDto
    {
        public string NpcId { get; set; } = null!;
        public int TotalSessions { get; set; }
        public double AverageFairness { get; set; }
        public ToneDistributionDto ToneDistribution { get; set; } = new();
        public double InCharacterRate { get; set; }
        public double EscalationRate { get; set; }
    }

}
