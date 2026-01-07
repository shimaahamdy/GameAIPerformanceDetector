namespace GameAi.Api.DTOs
{
    public class DeveloperMessageDto
    {
        public string Role { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }

}
