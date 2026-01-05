namespace GameAi.Api.Models
{
    public class NpcRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NpcId { get; set; } = null!;
        public string RuleText { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
