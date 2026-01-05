namespace GameAi.Api.DTOs
{
    public class SessionConversationTurnDto
    {
        public string PlayerMessage { get; set; } = null!;
        public string AiResponse { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }

}
