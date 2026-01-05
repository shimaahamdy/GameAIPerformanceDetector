namespace GameAi.Api.RAG.Models
{
    public class RagContextResult
    {
        public List<string> Rules { get; set; } = new();
        public List<string> SimilarConversations { get; set; } = new();
    }

}
