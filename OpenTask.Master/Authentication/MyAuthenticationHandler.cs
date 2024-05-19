using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Scheduler.Core;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace Scheduler.Master.Authentication
{
    /// <summary>
    /// 自定义认证
    /// </summary>
    public class MyAuthenticationHandler : AuthenticationHandler<MyAuthenticationSchemeOptions>
    {
        public MyAuthenticationHandler(
            IOptionsMonitor<MyAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // 验证集群
            if (!Request.Headers.TryGetValue(ConstString.HEADER_GROUP_NAME, out StringValues groupName))
            {
                return Task.FromResult(AuthenticateResult.Fail($"Header {ConstString.HEADER_GROUP_NAME} is Required"));
            };

            if (!Request.Headers.TryGetValue(ConstString.HEADER_CLIENT_ID, out StringValues clientID))
            {
                return Task.FromResult(AuthenticateResult.Fail($"Header {ConstString.HEADER_CLIENT_ID} is Required"));
            };

            Request.Headers.TryGetValue(ConstString.HEADER_TOKEN, out StringValues clusterSecret);

            // TODO：身份验证

            var claims = new[] {
                new Claim(ConstString.HEADER_GROUP_NAME, groupName),
                new Claim(ConstString.HEADER_CLIENT_ID, clientID),
            };

            var claimsIdentity = new ClaimsIdentity(claims, nameof(MyAuthenticationHandler));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(claimsIdentity), Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
