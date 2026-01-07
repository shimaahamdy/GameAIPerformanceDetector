using GameAi.Api.DTOs;
using GameAI.Context;
using Microsoft.EntityFrameworkCore;

namespace GameAi.Api.Services
{
    public interface IDeveloperMessageService
    {
        Task<List<DeveloperMessageDto>> GetMessagesAsync(string developerId, int page = 1, int pageSize = 20);
    }

    public class DeveloperMessageService : IDeveloperMessageService
    {
        private readonly GameAIContext _db;

        public DeveloperMessageService(GameAIContext db)
        {
            _db = db;
        }

        public async Task<List<DeveloperMessageDto>> GetMessagesAsync(string developerId, int page = 1, int pageSize = 20)
        {
            return await _db.DeveloperMessages
                .Where(m => m.DeveloperId == developerId)
                .OrderByDescending(m => m.Timestamp) // latest messages first
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new DeveloperMessageDto
                {
                    Role = m.Role,
                    Message = m.Content,
                    Timestamp = m.Timestamp
                })
                .ToListAsync();
        }
    }

}
