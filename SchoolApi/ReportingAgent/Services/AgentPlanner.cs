using GameAi.Api.ReportingAgent.Models;
using GameAi.Api.ReportingAgent.Services.Contracts;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GameAi.Api.ReportingAgent.Services
{

    public class AgentPlanner : IAgentPlanner
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AgentPlanner(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<AgentPlan> CreatePlanAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("User message cannot be null or empty.", nameof(userMessage));

            var client = _httpClientFactory.CreateClient("OpenAI");

            var systemMessage = new
            {
                role = "system",
                content = @"You are a game analytics planner AI. 
Convert developer requests into a structured plan. 
Output ONLY valid JSON with these exact fields:
- intent: one of ""session_report"", ""npc_analysis"", or ""comparison""
- sessionId: string or null (extract from user message if mentioned)
- npcId: string or null (extract from user message if mentioned)
- needsCharts: boolean (true if user asks for charts, graphs, or visualizations)
- needsPdf: boolean (true if user asks for pdf, report, export, or document)

Example: {""intent"":""session_report"",""sessionId"":""session-123"",""npcId"":null,""needsCharts"":true,""needsPdf"":false}"
            };

            var userMsg = new
            {
                role = "user",
                content = userMessage
            };

            var payload = new
            {
                model = "gpt-3.5-turbo-0125",
                messages = new[] { systemMessage, userMsg },
                temperature = 0,
                response_format = new { type = "json_object" }
            };

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
                throw new HttpRequestException($"Planner AI failed with status {response.StatusCode}: {raw}");

            // 1️ Extract AI response
            var aiContent = ExtractAiContent(raw);

            // 2️ Deserialize into AgentPlan
            try
            {
                var plan = JsonSerializer.Deserialize<AgentPlan>(aiContent);
                if (plan == null)
                    throw new InvalidOperationException("Failed to deserialize plan from AI response.");

                // Validate intent
                if (string.IsNullOrWhiteSpace(plan.Intent))
                    plan.Intent = AgentIntent.SessionReport;

                return plan;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse AI response as JSON: {ex.Message}. Raw content: {aiContent}", ex);
            }
        }

        private static string ExtractAiContent(string raw)
        {
            using var doc = JsonDocument.Parse(raw);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()!;

            // Trim extra text outside JSON
            var firstBrace = content.IndexOf('{');
            var lastBrace = content.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
                content = content.Substring(firstBrace, lastBrace - firstBrace + 1);

            return content;
        }
    }

}
