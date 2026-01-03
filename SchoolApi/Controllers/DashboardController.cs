using GameAi.Api.DTOs;
using GameAi.Api.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // 1️⃣ Get all sessions summary
    [HttpGet("sessions")]
    public async Task<ActionResult<List<DashboardConversationDto>>> GetSessions()
    {
        var result = await _dashboardService.GetSessionsAsync();
        return Ok(result);
    }

    // 2️⃣ Get session by ID with full conversation
    [HttpGet("sessions/detail")]
    public async Task<ActionResult<DashboardConversationDto>> GetSessionDetail(
            [FromQuery] string sessionId,
            [FromQuery] string playerId,
            [FromQuery] string npcId)
    {
        var session = await _dashboardService.GetSessionByIdAsync(sessionId, playerId, npcId);
        if (session == null) return NotFound();
        return Ok(session);
    }
}
