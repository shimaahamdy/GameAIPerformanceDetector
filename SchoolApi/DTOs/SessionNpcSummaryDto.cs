namespace GameAi.Api.DTOs
{
    public class SessionNpcSummaryDto
    {
        public string NpcId { get; set; } = null!;
        public string OverallTone { get; set; } = null!;
        public bool InCharacter { get; set; }
        public int FairnessScore { get; set; }
        public bool EscalationTooFast { get; set; }
        public string Summary { get; set; } = null!;
    }

}
