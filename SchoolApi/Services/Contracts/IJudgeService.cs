using GameAi.Api.DTOs;

namespace GameAi.Api.Services.Contracts
{
    public interface IJudgeService
    {
        Task<JudgeOutputDto> JudgeConversationAsync(JudgeInputDto input);
    }
}
