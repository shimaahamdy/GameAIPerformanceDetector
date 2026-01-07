using GameAi.Api.ReportingAgent.DTOs;

namespace GameAi.Api.DTOs
{
    public class DeveloperMessageWithResponseDto
    {
        public string Role { get; set; } = null!;
        public ChartsAgentChatResponse Response { get; set; } = null!;
        public string MessageText { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
