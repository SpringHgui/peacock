using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MQTTnet.Diagnostics;
using Scheduler.Core.Services;
using Scheduler.Entity.Data;
using Scheduler.Master.Extensions;
using Scheduler.Master.Filters;
using Scheduler.Master.Models;
using Scheduler.Master.Server;
using Scheduler.Master.Services;
using Serilog;
using Serilog.Events;
using System.Text.Json;

namespace Scheduler.Master
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //for (int i = 0; i < 1000; i++)
            //{
            //    var dd = Crc16.CalculateCRC16(i);
            //    Console.WriteLine(dd);
            //}

            const string outputTemplate = "[{Timestamp:HH:mm:ss} {RequestId} {Level:u3}] {Message:lj}{NewLine}{Exception}";

            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            builder.Services.AddSerilog((config) =>
            {
                config.MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Microsoft.Hosting.Lifetime
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                    .WriteTo.File("logs/.log", outputTemplate: outputTemplate, rollingInterval: RollingInterval.Hour, shared: true)
                    .WriteTo.Console(outputTemplate: outputTemplate);
            });

            builder.Services.AddAuthentication("Bearer").AddJwtBearer(delegate (JwtBearerOptions options)
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(Convert.ToInt32(configuration.GetSection("JWT")["ClockSkew"])),
                    ValidateIssuerSigningKey = true,
                    ValidAudience = configuration.GetSection("JWT")["ValidAudience"],
                    ValidIssuer = configuration.GetSection("JWT")["ValidIssuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetSection("JWT")["IssuerSigningKey"]))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = async delegate (MessageReceivedContext message)
                    {
                        message.Token = message.Request.Headers.Authorization.ToString();
                        if (message.Token != null)
                        {
                            message.Token = message.Token.Replace("Bearer ", "");
                        }

                        await Task.CompletedTask;
                    },
                    OnChallenge = async delegate (JwtBearerChallengeContext context)
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
                    }
                };
            });

            builder.Services.AddHealthChecks();
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
                //����Jwt��֤����,��������ͷ��Ϣ
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

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Value Bearer {token}",
                    Name = "Authorization",//jwtĬ�ϵĲ�������
                    In = ParameterLocation.Header,//jwtĬ�ϴ��Authorization��Ϣ��λ��(����ͷ��)
                    Type = SecuritySchemeType.ApiKey
                });
            });

            builder.Services.AddSchedulerService(builder.Configuration);
            //builder.Services.AddSignalR(options =>
            //{
            //    options.EnableDetailedErrors = true;
            //    options.MaximumParallelInvocationsPerClient = 100;
            //})
            ////.AddStackExchangeRedis() // TODO��������չ
            //.AddJsonProtocol(options =>
            //{
            //    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            //})
            //.AddHubOptions<JobExecutorHub>(hubOptions =>
            //{
            //    hubOptions.MaximumReceiveMessageSize = null;
            //});

            builder.Services.AddSingleton<IMqttNetLogger, MyLog>();
            builder.Services.AddSingleton<IDiscovery, DiscoveryFromDb>();
            builder.Services.AddSingleton<ExcuteJobHandler>();
            builder.Services.AddSingleton<SchedulerSystem>();

            builder.Services.AddSingleton<ServerSystem>();
            builder.Services.AddHostedService<MqttServerService>();
            builder.Services.AddSingleton<CustomExceptionFilterAttribute>();
            builder.Services.AddSingleton<HubContext>();
            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("defalut", policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithOrigins("http://127.0.0.1:5173");
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var context = services.GetRequiredService<BxjobContext>();
                context.Database.EnsureCreated();
            }

            app.UseStaticFiles();
            app.UseVueRouterHistory();

            app.MapHealthChecks("/healthz");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("defalut");

            //app.MapHub<JobExecutorHub>("/hubs/executor", options =>
            //{
            //    options.ApplicationMaxBufferSize = 0;
            //    options.TransportMaxBufferSize = 0;
            //});

            app.MapControllers();

            app.Run();
        }
    }
}