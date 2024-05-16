using MQTTnet.Diagnostics;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities;
using Qz.Utility.Extensions;
using Scheduler.Entity.Models;
using Scheduler.Master.Services;
using Scheduler.Service;
using System;
using System.Collections.Concurrent;
using System.Timers;
using System.Xml.Linq;
using static Scheduler.Master.Server.IDiscovery;

namespace Scheduler.Master.Server
{
    public class DiscoveryFromDb : IDiscovery
    {
        IServiceProvider service;
        System.Timers.Timer discoveryTimer;
        int times = 0;
        ILogger<DiscoveryFromDb> logger;
        MyMqttServer myMqttServer;

        public event OnNewNodeChange OnNewNodeConnected;
        public event OnNewNodeChange OnNodeDisconnected;

        public DiscoveryFromDb(IServiceProvider service, ILogger<DiscoveryFromDb> logger)
        {
            this.logger = logger;
            this.service = service;
            discoveryTimer = new System.Timers.Timer();
            discoveryTimer.Interval = 5000;
            discoveryTimer.Elapsed += discoveryTimerElapsed;
        }

        public Task StartAsync(MyMqttServer myMqttServer)
        {
            this.myMqttServer = myMqttServer;

            discoveryTimer.Start();
            return Task.CompletedTask;
        }

        private void discoveryTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            discoveryTimer.Enabled = false;
            times++;

            try
            {
                RegisterAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                logger.LogError(ex, null);
            }
            finally
            {
                discoveryTimer.Enabled = true;
            }
        }

        ConcurrentDictionary<string, MqttNode> nodes = new ConcurrentDictionary<string, MqttNode>();

        async Task RegisterAsync()
        {
            // 心跳写入数据库
            Update();

            if (times % 2 == 0)
            {
                // 服务发现
                var res = Discover();

                // 已经离线的节点
                var missNodes = this.nodes.Where(x => !res.Any(y => y.Guid == x.Key));

                bool changed = false;
                foreach (var item in res)
                {
                    if (nodes.TryAdd(item.Guid, item))
                    {
                        changed = true;
                        OnNewNodeConnected?.Invoke(item);
                    }
                }

                using var scope = service.CreateScope();
                var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();
                foreach (var item in missNodes)
                {
                    changed = true;
                    OnNodeDisconnected?.Invoke(item.Value);

                    serverService.Delete(new ScServer()
                    {
                        Id = item.Value.Id,
                        HeartAt = item.Value.HeartAt,
                        Guid = item.Value.Guid,
                        EndPoint = item.Value.Endpoint
                    });
                }

                if (changed)
                {
                    ReSlot(res);
                }
            }
        }

        const int SlotCount = 16384;

        private void ReSlot(IEnumerable<MqttNode> mqttNodes)
        {
            var nodes = mqttNodes.OrderBy(x => x.Id).ToArray();
            var perCount = (SlotCount - nodes.Count()) / nodes.Count();
            int start = 0;

            for (int i = 0; i < nodes.Length; i++)
            {
                var next = start + perCount;
                nodes[i].Slot = $"{start},{(i == (nodes.Length - 1) ? (SlotCount - 1) : next - 1)}";
                start = next;
            }

            using var scope = service.CreateScope();
            var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();
            serverService.UpdateSlot(nodes.Select(x => new ScServer
            {
                EndPoint = x.Endpoint,
                Guid = x.Guid,
                HeartAt = x.HeartAt,
                Slot = x.Slot,
                Id = x.Id,
            }));
        }

        IEnumerable<MqttNode> Discover()
        {
            using var scope = this.service.CreateScope();
            var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();

            var list = serverService.GetServerOnline(15);

            return list.Select(x => new MqttNode
            {
                Id = x.Id,
                Slot = x.Slot,
                Endpoint = x.EndPoint,
                HeartAt = x.HeartAt,
                Guid = x.Guid,
            });
        }

        public void Register()
        {
            using var scope = this.service.CreateScope();
            var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();

            serverService.RegisterOrUpdate(new ScServer
            {
                Id = 15,
                Guid = myMqttServer.guid,
                EndPoint = myMqttServer.ExternalUrl,
                HeartAt = DateTime.Now.ToTimestamp(),
            });
        }

        public void Update()
        {
            using var scope = this.service.CreateScope();
            var serverService = scope.ServiceProvider.GetRequiredService<ServerService>();

            serverService.RegisterOrUpdate(new ScServer
            {
                Guid = myMqttServer.guid,
                EndPoint = myMqttServer.ExternalUrl,
                HeartAt = DateTime.Now.ToTimestamp(),
            });
        }
    }
}
