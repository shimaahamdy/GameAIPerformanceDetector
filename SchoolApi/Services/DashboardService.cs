using GameAi.Api.DTOs;
using GameAi.Api.Services.Contracts;
using GameAI.Context;
using Microsoft.EntityFrameworkCore;

public class DashboardService : IDashboardService
{
    private readonly GameAIContext _dbContext;

    public DashboardService(GameAIContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 1️⃣ Get all sessions (summary only)
    public async Task<List<DashboardConversationDto>> GetSessionsAsync()
    {
        var conversations = await _dbContext.AiConversations
            .AsNoTracking()
            .ToListAsync();

        var judgeResults = await _dbContext.JudgeResults
            .AsNoTracking()
            .ToListAsync();

        return conversations
            .GroupBy(c => c.SessionId)
            .Select(g =>
            {
                var first = g.First();
                var judge = judgeResults
                    .Where(j => j.SessionId == g.Key)
                    .OrderByDescending(j => j.CreatedAt)
                    .FirstOrDefault();

                return new DashboardConversationDto
                {
                    SessionId = g.Key,
                    Player = first.PlayerId,
                    Npc = first.NpcId,
                    Tone = judge?.OverallTone?.ToUpper() ?? "NEUTRAL",
                    Fairness = judge?.FairnessScore ?? 5,
                    Escalation = judge?.EscalationTooFast == true ? "Too Fast" : "No",
                    Summary = judge?.Summary ?? "No AI evaluation"
                };
            })
            .OrderByDescending(r => r.SessionId)
            .ToList();
    }

    public async Task<DashboardConversationDto?> GetSessionByIdAsync(string sessionId, string playerId, string npcId)
    {
        var conversations = await _dbContext.AiConversations
            .AsNoTracking()
            .Where(c => c.SessionId == sessionId && c.PlayerId == playerId && c.NpcId == npcId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        if (!conversations.Any()) return null;

        var judge = await _dbContext.JudgeResults
            .AsNoTracking()
            .Where(j => j.SessionId == sessionId && j.PlayerId == playerId && j.NpcId == npcId)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync();

        var first = conversations.First();

        return new DashboardConversationDto
        {
            SessionId = sessionId,
            Player = playerId,
            Npc = npcId,
            Conversation = conversations
                .SelectMany(c => new[]
                {
                    $"[PLAYER] {c.PlayerMessage}",
                    $"[NPC] {c.AiResponse}"
                })
                .ToList(),
            Tone = judge?.OverallTone?.ToUpper() ?? "NEUTRAL",
            Fairness = judge?.FairnessScore ?? 5,
            Escalation = judge?.EscalationTooFast == true ? "Too Fast" : "No",
            Summary = judge?.Summary ?? "No AI evaluation available."
        };
    }
}
