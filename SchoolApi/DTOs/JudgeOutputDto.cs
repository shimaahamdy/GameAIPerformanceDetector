namespace GameAi.Api.DTOs
{
    public class JudgeOutputDto
    {
        public string OverallTone { get; set; } = null!;
        public bool InCharacter { get; set; }
        public int? FairnessScore { get; set; } // optional
        public bool? EscalationTooFast { get; set; } // optional
        public string? Summary { get; set; } // optional
    }
}
