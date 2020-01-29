﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyApi.Models;

namespace MyApi.Controllers
{
    
    [ApiController]
    public class UserController : ControllerBase
    {
        private AppDbContext db;
        public IConfiguration _config { get; }

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            db = context;
            _config = configuration;
        }

        [Authorize]
        [HttpGet("/info")]
        public IActionResult Info()
        {
            return Ok("Hello");
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
                    issuer: _config.GetValue<string>("TokenSettings:Issuer"),
                    audience: _config.GetValue<string>("TokenSettings:Audience"),
                    notBefore: now,
                    claims: identity.Claims,
                    expires: now.Add(TimeSpan.FromMinutes(_config.GetValue<int>("TokenSettings:Lifetime"))),
                    signingCredentials: new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_config.GetValue<string>("TokenSettings:Key"))),
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
            User person = db.users.FirstOrDefault(u => u.Email == user.Email && u.Password == user.Password);
            if (person != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, person.Email),
                };
                ClaimsIdentity claimsIdentity =
                new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
                return claimsIdentity;
            }

            // если пользователя не найдено
            return null;
        }
    }
}