namespace GameAi.Api.ReportingAgent.ChatRag
{
    public class DevVectorEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public float[] Embedding { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string DeveloperId { get; set; } = null!;
    }
}
