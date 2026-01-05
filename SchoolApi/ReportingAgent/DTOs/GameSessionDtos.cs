using Data.Models;

namespace GameAi.Api.ReportingAgent.DTOs
{
    public class GameSessionSummaryDto
    {
        public string SessionId { get; set; } = null!;
        public List<string> PlayerIds { get; set; } = new();
        public List<string> NpcIds { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class GameSessionDetailsDto
    {
        public string SessionId { get; set; } = null!;
        public List<string> PlayerIds { get; set; } = new();
        public List<string> NpcIds { get; set; } = new();
        public List<AiConversation> Conversations { get; set; } = new();
    }

}
