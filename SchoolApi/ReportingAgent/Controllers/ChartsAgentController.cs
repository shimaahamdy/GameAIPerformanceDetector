using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.ReportingAgent.Services.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GameAi.Api.ReportingAgent.Controllers
{
    [ApiController]
    [Route("api/reporting-agent")]
    public class ChartsAgentController : ControllerBase
    {
        private readonly IChartsAgentService _agent;

        public ChartsAgentController(IChartsAgentService agent)
        {
            _agent = agent;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChartsAgentChatRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Request body cannot be null." });

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message cannot be null or empty." });

            try
            {
                var response = await _agent.HandleAsync(request.Message);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { error = "Processing failed.", details = ex.Message });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }

}
