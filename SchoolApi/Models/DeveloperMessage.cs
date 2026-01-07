namespace GameAi.Api.Models
{
    public class DeveloperMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DeveloperId { get; set; } = null!;
        public string Role { get; set; } = null!; // "developer" or "agent"
        public string Content { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}
