namespace GameAi.Api.DTOs
{
    public class JudgeInputDto
    {
        public string NpcId { get; set; } = null!;
        public string NpcRole { get; set; } = null!;
        public string? PlayerId { get; set; }
        public string? SessionId { get; set; }
        public List<string>? Rules { get; set; }

        public List<ConversationTurnDto> Conversation { get; set; } = new();
    }

}
