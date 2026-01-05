using GameAi.Api.DTOs;
using GameAI.Context;
using System.Net.Http.Json;
using System.Text.Json;

public interface IRagService
{
    Task<List<string>> GetRelevantContextAsync(List<ConversationTurnDto> conversation);
}

public class RagService : IRagService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InMemoryVectorStore _vectorStore;

    public RagService(IHttpClientFactory httpClientFactory, InMemoryVectorStore vectorStore)
    {
        _httpClientFactory = httpClientFactory;
        _vectorStore = vectorStore;
    }

    public async Task<List<string>> GetRelevantContextAsync(List<ConversationTurnDto> conversation)
    {
        var client = _httpClientFactory.CreateClient("OpenAI");

        // Convert conversation turns into single string
        var queryText = string.Join("\n", conversation.Select(c => $"[{c.Speaker.ToUpper()}] {c.Message}"));

        // Request embedding for query
        var payload = new { input = queryText, model = "text-embedding-3-small" };
        var response = await client.PostAsJsonAsync("v1/embeddings", payload);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var vector = json!.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(x => x.GetSingle())
            .ToArray();

        // Search vector store for top 3 relevant context entries
        var results = _vectorStore.Search(vector, topK: 3);
        return results.Select(r => r.Text).ToList();
    }
}
