namespace GameAi.Api.DTOs
{
    public class ConversationTurnDto
    {
        public string Speaker { get; set; } = null!; // "player" or "npc"
        public string Message { get; set; } = null!;
        public object Timestamp { get; internal set; }
    }
}