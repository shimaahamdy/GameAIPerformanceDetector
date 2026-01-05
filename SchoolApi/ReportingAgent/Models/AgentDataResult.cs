using GameAi.Api.ReportingAgent.DTOs;

namespace GameAi.Api.ReportingAgent.Models
{
    public class AgentDataResult
    {
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<ChartDto> Charts { get; set; } = new();
    }

}
