using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scheduler.Master.Filters;
using Scheduler.Master.Models;
using System.Security.Claims;

namespace Scheduler.Master.Controllers
{
    [ApiController]
    [Authorize]
    [ServiceFilter(typeof(CustomExceptionFilterAttribute))]
    [Route("api/[controller]/[action]")]
    public class BaseApiController : ControllerBase
    {
        protected ResultData ResultData = new ResultData();

        //LoginUser loginUser;

        //protected LoginUser CurrentUser
        //{
        //    get
        //    {
        //        if (loginUser != null)
        //        {
        //            return loginUser;
        //        }

        //        loginUser = new LoginUser()
        //        {
        //            UserName = ((ClaimsIdentity)User.Identity).Name,
        //            UserId = long.Parse(((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value)
        //        };

        //        return loginUser;
        //    }
        //}
    }
}
