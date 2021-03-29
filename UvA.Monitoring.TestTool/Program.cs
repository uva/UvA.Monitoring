using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UvA.Monitoring.Shared;

namespace UvA.Monitoring.TestTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Poll("HvA").Wait();
            //conn.CreateSubscription("https://monitoring.datanose.nl/Event").Wait();
            //conn.GetSubscriptions().Wait();
            //conn.GetNotifications().Wait();
        }

        static async Task Poll(string tenant)
        {
            var config = LogTool.GetConfig().GetSection(tenant);
            var conn = new ManagementConnector(config);
            await conn.Connect();

            var allUrls = new List<string>();
            var checker = new StreamChecker(LogTool.CreateLogger<StreamChecker>(), config, config["TenantId"]);
            await checker.Connect();
            while (true)
            {
                var urls = await conn.GetContentUrls(DateTime.Now.AddMinutes(-150));
                urls = urls.Except(allUrls).ToArray();
                allUrls.AddRange(urls);
                File.WriteAllLines("urls.log", allUrls);

                foreach (var url in urls)
                    await checker.Check(url);

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }
}
