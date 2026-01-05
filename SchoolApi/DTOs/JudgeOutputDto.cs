using System.Text.Json.Serialization;

namespace GameAi.Api.DTOs
{
    public class JudgeOutputDto
    {
        [JsonPropertyName("overallTone")]
        public string OverallTone { get; set; } = null!;

        [JsonPropertyName("inCharacter")]
        public bool InCharacter { get; set; }

        [JsonPropertyName("fairnessScore")]
        public int? FairnessScore { get; set; }

        [JsonPropertyName("escalationTooFast")]
        public bool? EscalationTooFast { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

    }
}
