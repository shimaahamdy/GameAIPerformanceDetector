using System.Linq;
using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.Services.Contracts;
using GameAI.Context;
using Microsoft.EntityFrameworkCore;

namespace GameAi.Api.ReportingAgent.Services
{
    using GameAI.Context;
    using Microsoft.EntityFrameworkCore;

    public interface IGameSessionService
    {
        Task<List<GameSessionSummaryDto>> GetAllSessionsAsync();
        Task<GameSessionOverviewDto> GetSessionOverviewAsync(string sessionId);
        Task<List<NpcSessionSummaryDto>> GetSessionNpcsAsync(string sessionId);
        Task EndSessionAsync(string sessionId);
    }

    public class GameSessionService : IGameSessionService
    {
        private readonly GameAIContext _dbContext;
        private readonly IJudgeService _judgeService;

        public GameSessionService(GameAIContext dbContext, IJudgeService judgeService)
        {
            _dbContext = dbContext;
            _judgeService = judgeService;
        }

        // ------------------------------
        // 1️- List all sessions
        // ------------------------------
        public async Task<List<GameSessionSummaryDto>> GetAllSessionsAsync()
        {
            return await _dbContext.AiConversations
                .GroupBy(c => c.SessionId)
                .Select(g => new GameSessionSummaryDto
                {
                    SessionId = g.Key,
                    PlayerIds = g.Select(c => c.PlayerId).Distinct().ToList(),
                    NpcIds = g.Select(c => c.NpcId).Distinct().ToList(),
                    StartTime = g.Min(c => c.Timestamp),
                    EndTime = g.Max(c => c.Timestamp)
                })
                .ToListAsync();
        }

        // ------------------------------
        // 2️- Session overview + metrics
        // ------------------------------
        public async Task<GameSessionOverviewDto> GetSessionOverviewAsync(string sessionId)
        {
            var conversations = await _dbContext.AiConversations
                .Where(c => c.SessionId == sessionId)
                .ToListAsync();

            if (!conversations.Any())
                throw new Exception("Session not found.");

            var npcIds = conversations.Select(c => c.NpcId).Distinct().ToList();
            var judges = await _dbContext.JudgeResults
                .Where(j => j.SessionId == sessionId)
                .ToListAsync();

            // Session metrics
            var metrics = new Dictionary<string, object>
            {
                ["SessionId"] = sessionId,
                ["TotalNpcs"] = npcIds.Count,
                ["TotalMessages"] = conversations.Count,
                ["AvgFairness"] = judges.Any() ? Math.Round(judges.Average(j => j.FairnessScore), 2) : 5,
                ["EscalationRate"] = judges.Any() ? Math.Round(judges.Count(j => j.EscalationTooFast) / (double)judges.Count, 2) : 0
            };

            // NPC summaries (use the DTO shape that exists in the project)
            var npcSummaries = npcIds.Select(npcId =>
            {
                var npcJudges = judges.Where(j => j.NpcId == npcId).ToList();
                var npcConvs = conversations.Where(c => c.NpcId == npcId).ToList();

                return new NpcSessionSummaryDto
                {
                    NpcId = npcId,
                    OverallTone = npcJudges.FirstOrDefault()?.OverallTone ?? "neutral",
                    Fairness = npcJudges.FirstOrDefault()?.FairnessScore ?? 5,
                    Escalation = npcJudges.FirstOrDefault()?.EscalationTooFast ?? false,
                    TotalTurns = npcConvs.Count
                };
            }).ToList();

            return new GameSessionOverviewDto
            {
                SessionId = sessionId,
                PlayerIds = conversations.Select(c => c.PlayerId).Distinct().ToList(),
                NpcSummaries = npcSummaries,
                TotalTurns = conversations.Count
            };
        }

        // Return per-NPC summaries (used by AgentDataService)
        public async Task<List<NpcSessionSummaryDto>> GetSessionNpcsAsync(string sessionId)
        {
            var overview = await GetSessionOverviewAsync(sessionId);
            return overview.NpcSummaries;
        }

        // ------------------------------
        // 3️⃣ End session and run AI Judge for all NPCs
        // ------------------------------
        public async Task EndSessionAsync(string sessionId)
        {
            // Minimal implementation so the method exists.
            // If you want to automatically run judge logic here, implement calling IJudgeService and saving JudgeResults.
            await Task.CompletedTask;
        }
    }


}
