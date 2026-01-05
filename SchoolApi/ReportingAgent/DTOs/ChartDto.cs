namespace GameAi.Api.ReportingAgent.DTOs
{
    public class ChartDto
    {
        public string Type { get; set; } = ""; // bar, pie, line
        public string Title { get; set; } = "";
        public List<string> Labels { get; set; } = new();
        public List<double> Values { get; set; } = new();
    }

}
