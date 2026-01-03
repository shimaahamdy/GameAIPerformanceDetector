using Data.Models;
using System.ComponentModel.DataAnnotations;

namespace GameAI.DTOs
{
    public class AiConversationDto
    {
        [Required]
        [MaxLength(50)]
        public string SessionId { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string PlayerId { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string NpcId { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string PlayerMessage { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string AiResponse { get; set; } = null!;

        [Required]
        public DateTime Timestamp { get; set; }
    }
}
