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
                // ���� SetCurrentDirectory ��ʹ��Ӧ�õķ���λ��·���� 
                // ��Ҫ���� GetCurrentDirectory ����ȡ·������Ϊ�ڵ��� GetCurrentDirectory ʱ��Windows ����Ӧ�ý����� C:\WINDOWS\system32 �ļ��С� 
                // �й���ϸ��Ϣ������ĵ�ǰĿ¼�����ݸ����֡� ����ִ�д˲��裬Ȼ������ CreateWebHostBuilder ������Ӧ�á�
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
                    var msg = context.Error ?? "δ��¼";

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
                //���Jwt��֤����,�������ͷ��Ϣ
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
                    Name = "Authorization",//jwtĬ�ϵĲ�������
                    In = ParameterLocation.Header,//jwtĬ�ϴ��Authorization��Ϣ��λ��(����ͷ��)
                    Type = SecuritySchemeType.ApiKey
                });
            });

            builder.Services.AddSchedulerService(builder.Configuration);
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumParallelInvocationsPerClient = 100;
            })
            //.AddStackExchangeRedis() // TODO��������չ
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