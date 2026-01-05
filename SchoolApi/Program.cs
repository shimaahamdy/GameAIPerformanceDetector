using GameAi.Api.RAG;
using GameAi.Api.RAG.Services;
using GameAi.Api.RAG.Services.Contracts;
using GameAi.Api.Services;
using GameAi.Api.Services.Contracts;
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
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IRagService, RagService>();

            builder.Services.AddSingleton<VectorStore>();
            builder.Services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
            builder.Services.AddScoped<IRagQueryService, RagQueryService>();
            builder.Services.AddScoped<IRagSeeder, RagSeeder>();


            builder.Services.AddHttpClient("OpenAI", client =>
            {
                client.BaseAddress = new Uri("https://api.openai.com"); // or your AI endpoint
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
