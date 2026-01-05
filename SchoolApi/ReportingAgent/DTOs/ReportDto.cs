namespace GameAi.Api.ReportingAgent.DTOs
{
    public class ReportDto
    {
        public string FileName { get; set; } = "";
        public byte[] FileContent { get; set; } = Array.Empty<byte>();
    }

}
