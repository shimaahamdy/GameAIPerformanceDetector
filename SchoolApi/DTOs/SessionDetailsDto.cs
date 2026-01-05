namespace GameAi.Api.DTOs
{
    public class SessionDetailsDto
    {
        public string SessionId { get; set; } = null!;
        public string PlayerId { get; set; } = null!;
        public string NpcId { get; set; } = null!;

        public JudgeOutputDto Judge { get; set; } = null!;

        public List<SessionConversationTurnDto> Conversation { get; set; } = new();
    }

}
