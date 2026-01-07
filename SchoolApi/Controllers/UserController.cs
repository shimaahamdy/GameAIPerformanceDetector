using GameAi.Api.ReportingAgent.ChatRag;
using GameAI.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GameAI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private UserManager<IdentityUser> userManager;
        private readonly DeveloperAgentService _agent;


        public UserController(UserManager<IdentityUser> _userManager, DeveloperAgentService agent)
        {
            userManager = _userManager;
            _agent = agent;
        }

        //Register
        [HttpPost]
        public async Task<IActionResult> RegisterUser(UserRegisterDto userRegisterDto)
        {
            // register new user 
            var user = new IdentityUser();
            user.UserName = userRegisterDto.UserName;
            var result = await userManager.CreateAsync(user, userRegisterDto.Password);
            if (result.Succeeded)
                return Ok("created");
            return BadRequest(result.Errors.ToList());


        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserRegisterDto userLoginDto)
        {
            // check if user exist 
            // check password 
            var user = await userManager.FindByNameAsync(userLoginDto.UserName);
            if (user != null)
            {
                bool found = await userManager.CheckPasswordAsync(user, userLoginDto.Password);
                if (!found)
                    return Unauthorized();

                else
                {
                    var developerId = user.Id;

                    await _agent.InitializeDeveloperMemoryAsync(developerId);
                    // generate token

                    return Ok($"token: {GenerateToken(user)}");

                }
            }
            return BadRequest();
        }


        private string GenerateToken(IdentityUser user)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var token = new JwtSecurityToken(
                //payload
                issuer: "http://localhost:5034",
                audience: "http://localhost:5000",
                expires: DateTime.Now.AddMinutes(20),
                claims: claims,
                // signature 
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                   new SymmetricSecurityKey(Encoding.UTF8.GetBytes("superSecretKeyhjhjkhhkjhljkhlkjh@345")),
                   SecurityAlgorithms.HmacSha256
                   ));

            var returnToken = new JwtSecurityTokenHandler().WriteToken(token);


            return returnToken;
                
                

          
                
        }

    }
}
