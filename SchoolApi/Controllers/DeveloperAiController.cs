using GameAi.Api.DTOs;
using GameAi.Api.ReportingAgent.ChatRag;
using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace GameAi.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class DeveloperAiController : ControllerBase
    {
        private readonly DeveloperAgentService _agent;
        private readonly IDeveloperMessageService _service;

        public DeveloperAiController(DeveloperAgentService agent, IDeveloperMessageService service)
        {
            _agent = agent;
            _service = service;
        }


        [HttpGet]
        public async Task<IActionResult> GetMessages(int page = 1, int pageSize = 20)
        {
            // Extract developer ID from JWT
            var developerId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            var messages = await _service.GetMessagesAsync(developerId, page, pageSize);
            JsonSerializerOptions _jsonOptions =
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };


            var convertedMessages = messages.Select(m =>
            {
                if (m.Role.Equals("agent", StringComparison.OrdinalIgnoreCase))
                {
                    return new DeveloperMessageWithResponseDto
                    {
                        Role = m.Role,
                        Timestamp = m.Timestamp,
                        Response = JsonSerializer.Deserialize<ChartsAgentChatResponse>(
                            m.Message,
                            _jsonOptions
                        ) ?? throw new JsonException("Agent message is not valid JSON"),
                        MessageText = null
                    };
                }
                else
                {
                    return new DeveloperMessageWithResponseDto
                    {
                        Role = m.Role,
                        Timestamp = m.Timestamp,
                        Response = null,
                        MessageText = m.Message
                    };
                }
            }).ToList();

            return Ok(convertedMessages);
        }
    }

}
