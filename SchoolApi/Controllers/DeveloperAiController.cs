using GameAi.Api.DTOs;
using GameAi.Api.ReportingAgent.ChatRag;
using GameAi.Api.ReportingAgent.DTOs;
using GameAi.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost("request")]
        public async Task<IActionResult> PostRequest([FromBody] string userMessage)
        {
            // Get developer ID from identity
            var developerId = User.FindFirst("sub")?.Value
                              ?? User.FindFirst("id")?.Value
                              ?? throw new Exception("User not authenticated");

            var response = await _agent.HandleDeveloperRequestAsync(developerId, userMessage);
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int page = 1, int pageSize = 20)
        {
            // Extract developer ID from JWT
            var developerId = User.Identity.Name;

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
