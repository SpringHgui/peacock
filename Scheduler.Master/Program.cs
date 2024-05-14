using EasyAspNetCore.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scheduler.Master.Extensions;
using Scheduler.Master.Filters;
using Scheduler.Master.Hubs;
using Scheduler.Master.Models;
using Scheduler.Master.Services;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Text.Json;
using TimeCrontab;

namespace Scheduler.Master
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (WindowsServiceHelpers.IsWindowsService())
            {
                // 调用 SetCurrentDirectory 并使用应用的发布位置路径。 
                // 不要调用 GetCurrentDirectory 来获取路径，因为在调用 GetCurrentDirectory 时，Windows 服务应用将返回 C:\WINDOWS\system32 文件夹。 
                // 有关详细信息，请参阅当前目录和内容根部分。 请先执行此步骤，然后再在 CreateWebHostBuilder 中配置应用。
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                Directory.SetCurrentDirectory(pathToContentRoot);
            }

            var builder = WebApplication.CreateBuilder(args);

            builder.AddEasyAspNetCore(new EasyAspNetCore.EasyAspNetCoreOption
            {
                InvalidModelStateHandler = msg =>
                {
                    return new ResultData()
                    {
                        success = false,
                        message = msg
                    };
                },
                OnMessageReceived = async (message) =>
                {
                    message.Token = message.Request.Headers.Authorization.ToString();
                    if (message.Token != null)
                    {
                        message.Token = message.Token.Replace("Bearer ", "");
                    }

                    await Task.CompletedTask;
                },
                OnChallenge = async (context) =>
                {
                    context.HandleResponse();
                    context.Response.ContentType = "application/json;charset=utf-8";
                    var msg = context.Error ?? "未登录";

                    await context.Response.WriteAsync(JsonSerializer.Serialize(new ResultData
                    {
                        message = msg,
                        success = false,
                        trace_id = context.HttpContext.TraceIdentifier
                    }));
                },
                DisNewtonsoftJson = true
            });


            builder.Services.AddAuthentication().AddMyAuthentication();

            // Add services to the container.
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                //添加Jwt验证设置,添加请求头信息
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        },
                        new List<string>()
                    }
                });

                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "Value Bearer {token}",
                    Name = "Authorization",//jwt默认的参数名称
                    In = ParameterLocation.Header,//jwt默认存放Authorization信息的位置(请求头中)
                    Type = SecuritySchemeType.ApiKey
                });
            });

            builder.Services.AddSchedulerService(builder.Configuration);
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumParallelInvocationsPerClient = 100;
            })
            //.AddStackExchangeRedis() // TODO：横向扩展
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            })
            .AddHubOptions<JobExecutorHub>(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = null;
            });

            builder.Services.AddSingleton<ExcuteJobHandler>();
            builder.Services.AddSingleton<SchedulerSystem>();
            builder.Services.AddHostedService<SchedulerHostedService>();
            builder.Services.AddSingleton<CustomExceptionFilterAttribute>();
            builder.Host.UseWindowsService(options =>
                     {
                         options.ServiceName = "BXScheduler.Master";
                     });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles();
            app.UseVueRouterHistory();

            app.UseEasyAspNetCore();

            app.MapHub<JobExecutorHub>("/hubs/executor", options =>
            {
                options.ApplicationMaxBufferSize = 0;
                options.TransportMaxBufferSize = 0;
            });

            app.MapControllers();

            app.Run();
        }
    }
}