using GameAi.Api.RAG.Services.Contracts;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GameAi.Api.RAG.Services
{
    public class OpenAiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public OpenAiEmbeddingService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI API key not configured");
        }

        public async Task<float[]> CreateEmbeddingAsync(string text)
        {
            var payload = new
            {
                model = "text-embedding-3-small",
                input = text
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
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
