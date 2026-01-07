using System.Text.Json;

namespace GameAi.Api.ReportingAgent.DTOs
{
    public class ChartsAgentChatResponse
    {
        public string Text { get; set; } = "";
        public List<ChartDto> Charts { get; set; } = new();
        public ReportDto? Report { get; set; }
        public string Summary { get; set; } = string.Empty; 





        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

}
