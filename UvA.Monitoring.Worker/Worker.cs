using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UvA.Monitoring.Shared;

namespace UvA.Monitoring.Worker
{
    public class UvAWorker : Worker
    {
        public UvAWorker(IConfiguration config, ILogger<UvAWorker> logger) : base(config.GetSection("UvA"), logger) { }
    }

    public class HvAWorker : Worker
    {
        public HvAWorker(IConfiguration config, ILogger<HvAWorker> logger) : base(config.GetSection("HvA"), logger) { }
    }

    public abstract class Worker : BackgroundService
    {
        protected ILogger<Worker> Logger { get; init; }
        protected IConfiguration Config { get; init; }

        public Worker(IConfiguration config, ILogger<Worker> logger)
        {
            Logger = logger;
            Config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var start = DateTime.Now.ToUniversalTime().AddMinutes(-10);
            var log = Logger;

            while (!stoppingToken.IsCancellationRequested)
            {
                var conn = new ManagementConnector(Config);
                await conn.Connect();
                var checker = new StreamChecker(log, Config, Config["TenantId"]);
                await checker.Connect();

                var urls = await conn.GetContentUrls(start);
                start = DateTime.Now.ToUniversalTime();

                foreach (var url in urls)
                {
                    try
                    {
                        await checker.Check(url);
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, $"Error retrieving {url}");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }
}
