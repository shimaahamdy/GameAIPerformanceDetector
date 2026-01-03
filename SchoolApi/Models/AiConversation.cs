using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public class AiConversation
    {
        public Guid Id { get; set; }

        public string SessionId { get; set; } = null!;
        public string PlayerId { get; set; } = null!;
        public string NpcId { get; set; } = null!;

        public string PlayerMessage { get; set; } = null!;
        public string AiResponse { get; set; } = null!;

        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
