namespace GameAi.Api.RAG.Services.Contracts
{
    public interface IEmbeddingService
    {
        Task<float[]> CreateEmbeddingAsync(string text);
    }

}
