using GameAi.Api.DTOs;
using GameAi.Api.Models;
using GameAi.Api.Services.Contracts;
using GameAI.Context;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameAi.Api.Services
{
    public class JudgeService : IJudgeService
    {
        private readonly GameAIContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public JudgeService(GameAIContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<JudgeOutputDto> JudgeConversationAsync(JudgeInputDto input)
        {
            var client = _httpClientFactory.CreateClient("OpenAI");

            var modelName = "gpt-3.5-turbo-0125"; // safe, widely available

            // 1️⃣ System prompt: very explicit to force JSON only
            var systemMessage = new
            {
                role = "system",
                content =
                    "You are an AI judge. Evaluate the NPC-player conversation and return ONLY JSON. " +
                    "The JSON MUST have exactly these fields: " +
                    "overallTone (friendly, neutral, or hostile), " +
                    "inCharacter (true or false), " +
                    "fairnessScore (integer 0-10), " +
                    "escalationTooFast (true or false), " +
                    "summary (string). " +
                    "Do NOT add any extra text, explanation, or notes outside the JSON."
            };

            var userMessage = new
            {
                role = "user",
                content = JsonSerializer.Serialize(input)
            };

            var payload = new
            {
                model = modelName,
                messages = new[] { systemMessage, userMessage },
                temperature = 0.2
            };

            // 2️⃣ Serialize payload and call OpenAI
            var jsonPayload = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("v1/chat/completions", httpContent);
            var resultString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"AI Judge failed: {response.StatusCode}, {resultString}");
            }

            // 3️⃣ Extract AI content
            string aiContent;
            try
            {
                using var doc = JsonDocument.Parse(resultString);
                aiContent = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()!;
            }
            catch
            {
                aiContent = string.Empty;
            }

            // 4️⃣ Deserialize AI JSON safely
            JudgeOutputDto judgeResult;
            try
            {
                // Trim and extract JSON braces
                aiContent = aiContent.Trim();
                var firstBrace = aiContent.IndexOf('{');
                var lastBrace = aiContent.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                    aiContent = aiContent.Substring(firstBrace, lastBrace - firstBrace + 1);

                // Unescape inner JSON if double-escaped
                aiContent = Regex.Unescape(aiContent);

                judgeResult = JsonSerializer.Deserialize<JudgeOutputDto>(aiContent)!;
            }
            catch
            {
                judgeResult = new JudgeOutputDto
                {
                    OverallTone = "neutral",
                    InCharacter = true,
                    FairnessScore = 5,
                    EscalationTooFast = false,
                    Summary = "AI returned invalid JSON."
                };
            }

            // 5️⃣ Ensure no null fields
            judgeResult.OverallTone ??= "neutral";
            judgeResult.InCharacter = true;
            judgeResult.FairnessScore ??= 5;
            judgeResult.EscalationTooFast ??= false;
            judgeResult.Summary ??= "No summary provided by AI.";

            // 6️⃣ Save JudgeResult to DB
            var judgeEntity = new JudgeResult
            {
                Id = Guid.NewGuid(),
                SessionId = input.SessionId ?? "unknown",
                PlayerId = input.PlayerId ?? "unknown",
                NpcId = input.NpcId,
                OverallTone = judgeResult.OverallTone,
                InCharacter = judgeResult.InCharacter,
                FairnessScore = judgeResult.FairnessScore.Value,
                EscalationTooFast = judgeResult.EscalationTooFast.Value,
                Summary = judgeResult.Summary,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.JudgeResults.Add(judgeEntity);
            await _dbContext.SaveChangesAsync();

            return judgeResult;
        }
    }
}
