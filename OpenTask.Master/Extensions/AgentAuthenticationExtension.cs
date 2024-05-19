using Microsoft.AspNetCore.Authentication;
using Scheduler.Master.Authentication;

namespace Scheduler.Master.Extensions
{
    public static class AgentAuthenticationExtension
    {
        public static void AddMyAuthentication(this AuthenticationBuilder builder)
        {
            builder.AddScheme<MyAuthenticationSchemeOptions, MyAuthenticationHandler>(MYAuthSchemeConstants.AuthenticationScheme, options =>
            {
                // nothing
            });
        }
    }
}
