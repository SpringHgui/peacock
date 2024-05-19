using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Scheduler.Master.Models;

namespace Scheduler.Master.Filters
{
    public class CustomExceptionFilterAttribute : ExceptionFilterAttribute
    {
        readonly ILogger<CustomExceptionFilterAttribute> _logger;

        public CustomExceptionFilterAttribute(ILogger<CustomExceptionFilterAttribute> logger)
        {
            _logger = logger;
        }

        public override void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "【全局异常捕获】");
            var res = new ResultData()
            {
                success = false,
                message = context.Exception.Message,
                trace_id = context.HttpContext.TraceIdentifier
            };

            var result = new JsonResult(res) { StatusCode = 200 };

            context.Result = result;
            context.ExceptionHandled = true;
        }
    }
}
