namespace GameAi.Api.DTOs
{
    public class NpcSessionSummaryDto
    {
        public string SessionId { get; set; } = null!;
        public string PlayerId { get; set; } = null!;
        public string Tone { get; set; } = null!;
        public int Fairness { get; set; }
        public bool InCharacter { get; set; }
        public bool EscalationTooFast { get; set; }
        public string Summary { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

}
