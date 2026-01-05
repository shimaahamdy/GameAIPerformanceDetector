using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Models;
using GameAi.Api.ReportingAgent.Services.Contracts;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GameAi.Api.ReportingAgent.Services
{
    public class AgentAiService : IAgentAiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPdfGenerator _pdfGenerator;

        public AgentAiService(IHttpClientFactory httpClientFactory, IPdfGenerator pdfGenerator)
        {
            _httpClientFactory = httpClientFactory;
            _pdfGenerator = pdfGenerator;
        }

        public async Task<ChartsAgentChatResponse> GenerateResponseAsync(string userMessage, AgentPlan plan, AgentDataResult data)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be null or empty.", nameof(userMessage));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // 1️⃣ Prepare AI prompt
            var systemMessage = new
            {
                role = "system",
                content = """
                You are a game analytics assistant.
                Using the metrics and chart data, generate:
                - A clear, concise developer report.
                - Summarize insights and trends.
                - Reference specific numbers from the data provided.
                - Do NOT make up numbers - only use what's provided.
                """
            };

            // 2️⃣ Format metrics for AI readability
            var metricsText = data.Metrics.Any() 
                ? string.Join("\n", data.Metrics.Select(kv => $"{kv.Key}: {kv.Value}"))
                : "No metrics available.";

            // 3️⃣ Format charts with full data for AI readability
            var chartsText = data.Charts.Any()
                ? string.Join("\n\n", data.Charts.Select(c => 
                    $"- {c.Title} ({c.Type} chart):\n  Labels: {string.Join(", ", c.Labels)}\n  Values: {string.Join(", ", c.Values)}"))
                : "No charts available.";

            // 4️⃣ Prepare user message
            var userContent = new
            {
                role = "user",
                content = $"""
                User request: {userMessage}

                Metrics:
                {metricsText}

                Charts Data:
                {chartsText}
                """
            };

            var payload = new
            {
                model = "gpt-3.5-turbo-0125",
                messages = new[] { systemMessage, userContent },
                temperature = 0.3
            };

            var client = _httpClientFactory.CreateClient("OpenAI");
            var httpContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await HttpRetryHelper.ExecuteWithRetryAsync(
                client,
                () => new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                },
                maxRetries: 3,
                baseDelayMs: 1000);

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"AI explanation failed with status {response.StatusCode}: {raw}");

            // 5️⃣ Extract AI text safely
            string aiText = ExtractAiContentSafe(raw);

            // 6️⃣ Generate PDF if requested in plan (not duplicate check)
            ReportDto? pdfReport = null;
            if (plan.NeedsPdf)
            {
                try
                {
                    var pdfBytes = await _pdfGenerator.GeneratePdfAsync(aiText, data.Charts);
                    pdfReport = new ReportDto
                    {
                        FileName = $"GameReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
                        FileContent = pdfBytes
                    };
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the entire request
                    // PDF generation failure shouldn't prevent text response
                    throw new InvalidOperationException($"PDF generation failed: {ex.Message}", ex);
                }
            }

            return new ChartsAgentChatResponse
            {
                Text = aiText,
                Charts = data.Charts,
                Report = pdfReport
            };
        }

        // -------------------------------
        // Safely extract AI content from OpenAI response
        // -------------------------------
        private static string ExtractAiContentSafe(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                
                if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    throw new InvalidOperationException("AI response does not contain choices array or choices array is empty.");

                var firstChoice = choices[0];
                if (!firstChoice.TryGetProperty("message", out var message))
                    throw new InvalidOperationException("AI response choice does not contain message property.");

                if (!message.TryGetProperty("content", out var contentElement))
                    throw new InvalidOperationException("AI response message does not contain content property.");

                var content = contentElement.GetString();
                return content?.Trim() ?? "AI returned empty response.";
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse AI response JSON: {ex.Message}", ex);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error parsing AI response: {ex.Message}", ex);
            }
        }
    }
}
