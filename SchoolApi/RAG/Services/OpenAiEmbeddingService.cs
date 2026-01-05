using GameAi.Api.RAG.Services.Contracts;
using System.Text.Json;

namespace GameAi.Api.RAG.Services
{
    public class OpenAiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _http;

        public OpenAiEmbeddingService(HttpClient http)
        {
            _http = http;
        }

        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            var payload = new
            {
                model = "text-embedding-3-small",
                input = text
            };

            var response = await _http.PostAsJsonAsync("https://api.openai.com/v1/embeddings", payload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            return json
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();
        }
    }

}
