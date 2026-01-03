using System.ComponentModel.DataAnnotations;

namespace GameAi.Api.Models
{
    public class JudgeResult
    {
        [Key]
        public Guid Id { get; set; }

        // Link to conversation
        [Required]
        [MaxLength(50)]
        public string SessionId { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string PlayerId { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string NpcId { get; set; } = null!;

        // Conversation-level judgment
        [Required]
        [MaxLength(50)]
        public string OverallTone { get; set; } = null!; // friendly / neutral / hostile

        [Required]
        public bool InCharacter { get; set; } // true/false

        public int FairnessScore { get; set; } // 0–10 scale

        public bool EscalationTooFast { get; set; } // optional flag

        [MaxLength(1000)]
        public string Summary { get; set; } = null!; // optional note

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
