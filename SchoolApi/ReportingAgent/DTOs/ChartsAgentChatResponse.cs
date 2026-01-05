namespace GameAi.Api.ReportingAgent.DTOs
{
    public class ChartsAgentChatResponse
    {
        public string Text { get; set; } = "";
        public List<ChartDto> Charts { get; set; } = new();
        public ReportDto? Report { get; set; }
    }

}
