using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Scheduler.Entity.Models;
using Scheduler.Master.Models;
using Scheduler.Service;
using Serilog.Parsing;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Policy;

namespace Scheduler.Master.Controllers
{
    public class USerController : BaseApiController
    {
        IConfiguration configuration;
        public USerController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpPost]
        [AllowAnonymous]
        public ResultData GetToken([FromServices] UserService userService, GetTokenRequest request)
        {
            if (!userService.CheckPassWord(request.UserName, request.PassWord))
            {
                throw new Exception("用户名或密码错误");
            }

            var user = userService.GetUserByUserName(request.UserName);
            if (user == null)
            {
                throw new Exception("获取用户失败");
            }

            var claims = new Claim[] {
                new Claim("USER", System.Text.Json.JsonSerializer.Serialize(user)),
            };

            var token = GenerateToken(claims, 240, configuration);
            ResultData.data = "Bearer " + token;

            return ResultData;
        }

        protected string GenerateToken(IEnumerable<Claim> claims, int expiresMinutes, IConfiguration configuration)
        {
            var Secret = configuration.GetSection("JWT")["IssuerSigningKey"];
            var issuer = configuration.GetSection("JWT")["ValidIssuer"];
            var audience = configuration.GetSection("JWT")["ValidAudience"];

            var key = new SymmetricSecurityKey(Convert.FromBase64String(Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var securityToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(expiresMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(securityToken);
        }

    }
}
