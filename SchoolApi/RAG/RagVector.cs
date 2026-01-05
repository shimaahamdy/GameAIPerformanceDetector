namespace GameAi.Api.RAG
{
    public class RagVector
    {
        public string Id { get; set; } = null!;
        public float[] Embedding { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string SourceType { get; set; } = null!; // Rule / Conversation
        public string? NpcId { get; set; }
        public string? SessionId { get; set; }
    }
  

}
