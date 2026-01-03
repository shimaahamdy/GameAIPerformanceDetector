namespace GameAi.Api.DTOs
{
    public class DashboardConversationDto
    {
        public string SessionId { get; set; } = default!;
        public string Player { get; set; } = default!;
        public string Npc { get; set; } = default!;

        public List<string> Conversation { get; set; } = new();

        public string Tone { get; set; } = "NEUTRAL";
        public int Fairness { get; set; } = 5;
        public string Escalation { get; set; } = "No";
        public string Summary { get; set; } = string.Empty;
    }
}
