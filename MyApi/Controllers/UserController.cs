using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyApi.Models;

namespace MyApi.Controllers
{
    
    [ApiController]
    public class UserController : ControllerBase
    {
        private AppDbContext db;
        public TokenSettings _tokenSettings { get; }
        private ILogger _logger;


        public UserController(AppDbContext context, IOptions<TokenSettings> options, ILoggerFactory loggerFactory)
        {
            db = context;
            _tokenSettings = options.Value;
            _logger = loggerFactory.CreateLogger("FileLogger");
        }
        
        [Authorize]
        [HttpGet("/info")]
        public IActionResult Info()
        {
            _logger.LogInformation("HERE");
            return Ok("Hello "+ User.Identity.Name);
            
        }

        [Authorize(Roles="Admin")]
        [HttpGet("/role")]
        public IActionResult Role()
        {

            return Ok("Hello " + User.Identity.Name);

        }

        [HttpPost("/token")]
        public IActionResult Token(User user)
        {
            var identity = GetIdentity(user);
            if (identity == null)
            {
                return BadRequest(new { errorText = "Invalid email or password." });
            }

            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                    issuer: _tokenSettings.Issuer,
                    audience: _tokenSettings.Audience,
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromMinutes(_tokenSettings.Lifetime)),
                    signingCredentials: new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_tokenSettings.Key)),
                        SecurityAlgorithms.HmacSha256)
                    );

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var response = new
            {
                access_token = encodedJwt,
                username = identity.Name
            };

            return Ok(response);
        }

        private ClaimsIdentity GetIdentity(User user)
        {
            User person = db.users.Where(u => u.Email == user.Email && u.Password == user.Password).Include(u => u.Role).First();

            if (person != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, person.Email),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, person.Role?.Name),
                };

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                    claims, 
                    "Token", 
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType
                );
                return claimsIdentity;
            }

            return null;
        }
    }
}