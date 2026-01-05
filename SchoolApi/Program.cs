using GameAi.Api.RAG;
using GameAi.Api.RAG.Services;
using GameAi.Api.RAG.Services.Contracts;
using GameAi.Api.Services;
using GameAi.Api.Services.Contracts;
using GameAi.Api.ReportingAgent.Services;
using GameAi.Api.ReportingAgent.Services.Contracts;
using GameAI.Context;
using GameAI.Controllers;
using GameAI.MiddleWares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
namespace GameAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // apis use only contollers services without views 
            builder.Services.AddControllers();
   

            // Swagger 
            builder.Services.AddSwaggerGen();


            builder.Services.AddDbContext<GameAIContext>
                (options => options.UseSqlServer(@"Server=.;Database=GameAI;Trusted_Connection=True;Encrypt=false"));


            builder.Services.AddIdentity<IdentityUser, IdentityRole>(
                options =>
                {
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;

                }).AddEntityFrameworkStores<GameAIContext>();

            builder.Services.AddScoped<IJudgeService, JudgeService>();
            builder.Services.AddScoped<INpcAnalyticsService, NpcAnalyticsService>();
            builder.Services.AddScoped<INpcSessionService, NpcSessionService>();
            builder.Services.AddScoped<IRagService, RagService>();

            builder.Services.AddSingleton<VectorStore>();
            builder.Services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
            builder.Services.AddScoped<IRagQueryService, RagQueryService>();
            builder.Services.AddScoped<IRagSeeder, RagSeeder>();

            // ReportingAgent Services
            builder.Services.AddScoped<IGameSessionService, GameSessionService>();
            builder.Services.AddScoped<IAgentPlanner, AgentPlanner>();
            builder.Services.AddScoped<IAgentDataService, AgentDataService>();
            builder.Services.AddScoped<IAgentAiService, AgentAiService>();
            builder.Services.AddScoped<IPdfGenerator, QuestPdfGenerator>();

            // ReAct Agent Tools
            builder.Services.AddScoped<GameAi.Api.ReportingAgent.Services.Contracts.IReActTool, GameAi.Api.ReportingAgent.Services.Tools.GetSessionDataTool>();
            builder.Services.AddScoped<GameAi.Api.ReportingAgent.Services.Contracts.IReActTool, GameAi.Api.ReportingAgent.Services.Tools.GenerateChartsTool>();

            // Choose agent implementation:
            // Option 1: Original linear agent (Plan → Execute → Generate)
            // builder.Services.AddScoped<IChartsAgentService, ChartsAgentService>();
            
            // Option 2: ReAct agent (Reason → Act → Observe → Repeat)
            builder.Services.AddScoped<IChartsAgentService, ReActAgentService>();

            // Configure OpenAI HttpClient with API key from configuration
            var openAiApiKey = builder.Configuration["OpenAI:ApiKey"] 
                ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured in appsettings.json");
            var openAiBaseUrl = builder.Configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com";

            builder.Services.AddHttpClient("OpenAI", client =>
            {
                client.BaseAddress = new Uri(openAiBaseUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiApiKey}");
            });



            //Cors corss resources across many orgins 
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("allowAll", policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });


            builder.Services.AddAuthentication(op =>
            {
                op.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                op.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                op.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.SaveToken = true;
                o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = "http://localhost:5034",
                    ValidAudience = "http://localhost:5000",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("superSecretKeyhjhjkhhkjhljkhlkjh@345"))


                };
            }
            );

            var app = builder.Build();



            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                //app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseMiddleware<loggingMiddleWare>();
            app.UseCors("allowAll");

            

            //// need to use authentiacation 
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            app.Lifetime.ApplicationStarted.Register(async () =>
            {
                using var scope = app.Services.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<IRagSeeder>();
                await seeder.SeedAsync();
            });

            app.Run();
        }
    }
}
