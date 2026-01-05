using System.ComponentModel.DataAnnotations;

namespace GameAi.Api.Models
{
    public class JudgeResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SessionId { get; set; } = null!;
        public string PlayerId { get; set; } = null!;
        public string NpcId { get; set; } = null!;
        public string OverallTone { get; set; } = null!;
        public bool InCharacter { get; set; }
        public int FairnessScore { get; set; }
        public bool EscalationTooFast { get; set; }
        public string Summary { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
