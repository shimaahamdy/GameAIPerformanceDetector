using GameAi.Api.DTOs;
using GameAi.Api.Models;
using GameAi.Api.Services.Contracts;
using GameAI.Context;

namespace GameAi.Api.Services
{
    public class MockJudgeService : IJudgeService
    {
        private readonly GameAIContext _dbContext;

        public MockJudgeService(GameAIContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<JudgeOutputDto> JudgeConversationAsync(JudgeInputDto input)
        {
            // Simple mock logic: count hostile words
            var hostileWords = new List<string> { "no", "nothing", "refuse" };
            int hostileCount = input.Conversation
                .Where(x => x.Speaker == "npc")
                .Sum(x => hostileWords.Count(w => x.Message.Contains(w, StringComparison.OrdinalIgnoreCase)));

            var output = new JudgeOutputDto
            {
                OverallTone = hostileCount > 0 ? "hostile" : "neutral",
                InCharacter = true, // always true in mock
                FairnessScore = hostileCount > 2 ? 3 : 7,
                EscalationTooFast = hostileCount > 1,
                Summary = "This is a mock evaluation for testing purposes."
            };

            // Save to DB
            var judgeResult = new JudgeResult
            {
                Id = Guid.NewGuid(),
                SessionId = input.SessionId ?? "unknown",
                PlayerId = input.PlayerId ?? "unknown",
                NpcId = input.NpcId,
                OverallTone = output.OverallTone,
                InCharacter = output.InCharacter,
                FairnessScore = output.FairnessScore ?? 5,
                EscalationTooFast = output.EscalationTooFast ?? false,
                Summary = output.Summary ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.JudgeResults.Add(judgeResult);
            await _dbContext.SaveChangesAsync();

            return output;
        }
    }
}
