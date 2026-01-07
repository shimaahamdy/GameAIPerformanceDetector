using Data.Models;
using GameAi.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GameAI.Context
{
    public class GameAIContext : IdentityDbContext
    {

        public GameAIContext(DbContextOptions<GameAIContext> options): base(options)
        {
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        public DbSet<AiConversation> AiConversations { get; set; }
        public DbSet<JudgeResult> JudgeResults { get; set; }
        public DbSet<NpcRule> NpcRules { get; set; }
        public DbSet<DeveloperMessage> DeveloperMessages { get; set; }


    }
}
