using Data.Models;
using GameAi.Api.DTOs;
using GameAi.Api.Models;
using GameAi.Api.RAG.Models;
using GameAi.Api.RAG.Services.Contracts;
using GameAi.Api.Services.Contracts;
using GameAI.Context;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

namespace GameAi.Api.Services
{
    public class JudgeService : IJudgeService
    {
        private readonly GameAIContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IRagQueryService _ragQueryService;

        public JudgeService(GameAIContext dbContext, IHttpClientFactory httpClientFactory, IRagQueryService ragQueryService)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _ragQueryService = ragQueryService;

        }

        public async Task<JudgeOutputDto> JudgeConversationAsyncAPI(JudgeInputDto input)
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


        public async Task<JudgeOutputDto> JudgeConversationAsync(JudgeInputDto input)
        {
            // 1️ Load conversation for this session, NPC, player
            var conversation = await _dbContext.AiConversations
                .Where(c => c.SessionId == input.SessionId &&
                            c.NpcId == input.NpcId &&
                            c.PlayerId == input.PlayerId)
                .OrderBy(c => c.Timestamp)
                .ToListAsync();

            if (!conversation.Any())
                throw new Exception("No conversation found for this session.");

            // 2️⃣ Get RAG context (rules + past sessions)
            var ragContext = await _ragQueryService.QueryAsync(
                input.NpcId,
                input.SessionId,
                conversation,
                topK: 6);

            // 3️⃣ Build system prompt
            var systemPrompt = BuildSystemPrompt(ragContext);

            // 4️⃣ Build user payload including conversation & rules
            var userPayload = new
            {
                SessionId = input.SessionId,
                PlayerId = input.PlayerId,
                NpcId = input.NpcId,
                Conversation = BuildUserPrompt(conversation),
                Rules = ragContext.Rules
            };

            var userMessage = new
            {
                role = "user",
                content = JsonSerializer.Serialize(userPayload)
            };

            // 5️⃣ Call OpenAI
            var client = _httpClientFactory.CreateClient("OpenAI");

            var payload = new
            {
                model = "gpt-3.5-turbo-0125",
                temperature = 0.2,
                messages = new[] { new { role = "system", content = systemPrompt }, userMessage }
            };

            var httpContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("v1/chat/completions", httpContent);
            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Judge AI failed: {raw}");

            // 6️⃣ Extract JSON from AI response robustly
            string aiContent;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                aiContent = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()!;

                // Trim and ensure valid JSON substring
                var firstBrace = aiContent.IndexOf('{');
                var lastBrace = aiContent.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                    aiContent = aiContent.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            catch
            {
                aiContent = string.Empty;
            }

            // 7️⃣ Deserialize into JudgeOutputDto
            JudgeOutputDto output;
            try
            {
                output = JsonSerializer.Deserialize<JudgeOutputDto>(aiContent)!;
            }
            catch
            {
                output = Fallback();
            }

            // 8️⃣ Persist result in DB
            var entity = new JudgeResult
            {
                Id = Guid.NewGuid(),
                SessionId = input.SessionId,
                PlayerId = input.PlayerId,
                NpcId = input.NpcId,
                OverallTone = output.OverallTone,
                InCharacter = output.InCharacter,
                FairnessScore = output.FairnessScore ?? 5,
                EscalationTooFast = output.EscalationTooFast ?? false,
                Summary = output.Summary ?? "",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.JudgeResults.Add(entity);
            await _dbContext.SaveChangesAsync();

            return output;
        }

        // Helper: Build conversation as readable string for AI
        private string BuildUserPrompt(List<AiConversation> conversation)
        {
            var sb = new StringBuilder();
            foreach (var turn in conversation.OrderBy(c => c.Timestamp))
            {
                if (!string.IsNullOrEmpty(turn.PlayerMessage))
                    sb.AppendLine($"[PLAYER] {turn.PlayerMessage}");

                if (!string.IsNullOrEmpty(turn.AiResponse))
                    sb.AppendLine($"[NPC] {turn.AiResponse}");
            }
            return sb.ToString();
        }

        // Helper: Build system prompt with rules + past sessions
        private static string BuildSystemPrompt(RagContextResult rag)
        {
            return $"""
        You are an AI judge evaluating NPC behavior in a role-playing game.

        NPC RULES:
        {string.Join("\n", rag.Rules)}

        RELEVANT PAST SESSIONS:
        {string.Join("\n---\n", rag.SimilarConversations)}

        Your task:
        Evaluate the CURRENT conversation only.

        Return ONLY valid JSON with EXACT fields:
        overallTone (friendly | neutral | hostile)
        inCharacter (true | false)
        fairnessScore (0-10)
        escalationTooFast (true | false)
        summary (string)

        Do NOT include any explanation outside JSON.
        """;
        }

        // Fallback result if AI output fails
        private static JudgeOutputDto Fallback() => new()
        {
            OverallTone = "neutral",
            InCharacter = true,
            FairnessScore = 5,
            EscalationTooFast = false,
            Summary = "AI output invalid."
        };
       

    }


}
