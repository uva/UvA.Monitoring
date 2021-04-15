using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UvA.Monitoring.Shared
{
    public class ManagementConnector
    {
        IConfiguration Configuration;
        HttpClient Client;
        string TenantId;

        public ManagementConnector(IConfiguration config)
        {
            Configuration = config;
        }

        public async Task Connect()
        {
            TenantId = Configuration["TenantId"];

            var app = ConfidentialClientApplicationBuilder
               .Create(Configuration["AppId"])
               .WithTenantId(TenantId)
               .WithClientSecret(Configuration["AppSecret"])
               .Build();
            var res = await app.AcquireTokenForClient(new[] { "https://manage.office.com/.default" }).ExecuteAsync();

            Client = new HttpClient()
            {
                BaseAddress = new Uri($"https://manage.office.com/api/v1.0/{TenantId}/activity/feed/")
            };
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", res.AccessToken);
        }

        string DefaultParams => $"contentType=Audit.General&PublisherIdentifier={TenantId}";

        public async Task CreateSubscription(string webhook)
        {
            var res = await Client.PostJsonAsync($"subscriptions/start?{DefaultParams}", new 
            {
                webhook = new
                {
                    address = webhook
                }
            });
        }

        public async Task StopSubscription()
        {
            var res = await Client.PostJsonAsync($"subscriptions/stop?{DefaultParams}", new
            {
            });
        }

        public async Task GetSubscriptions()
        {
            var json = await Client.GetStringAsync($"subscriptions/list?{DefaultParams}");
        }

        /// <summary>
        /// For diagnosing webhook issues
        /// </summary>
        public async Task GetNotifications()
        {
            var windowStart = new DateTime(2021, 3, 28, 8, 0, 0);
            var windowEnd = windowStart.AddHours(8);
            var json = await Client.GetStringAsync($"subscriptions/notifications?{DefaultParams}&startTime={windowStart:yyyy-MM-dd}T{windowStart:HH:mm}&endTime={windowEnd:yyyy-MM-dd}T{windowEnd:HH:mm}");
        }

        public async Task<string[]> GetContentUrls(DateTime windowStart)
        {
            var windowEnd = windowStart.AddHours(8);
            var json = await Client.GetStringAsync($"subscriptions/content?{DefaultParams}&startTime={windowStart:yyyy-MM-dd}T{windowStart:HH:mm}&endTime={windowEnd:yyyy-MM-dd}T{windowEnd:HH:mm}");
            var entries = JsonDocument.Parse(json).RootElement.EnumerateArray().ToArray();
            return entries.Select(e => e.GetProperty("contentUri").GetString()).ToArray();
        }

        /// <summary>
        /// For testing purposes
        /// </summary>
        public async Task GetContent(DateTime windowStart)
        {
            var windowEnd = windowStart.AddHours(8);
            var json = await Client.GetStringAsync($"subscriptions/content?{DefaultParams}&startTime={windowStart:yyyy-MM-dd}T{windowStart:HH:mm}&endTime={windowEnd:yyyy-MM-dd}T{windowEnd:HH:mm}");
            var test = JsonDocument.Parse(json).RootElement.EnumerateArray().ToArray();
            Console.WriteLine($"{test.Count()} entries");
            int entry = 0;
            var allRecords = new List<AuditRecord>();
            while (entry < test.Length)
            {
                Console.Write($"Get entry {entry}: ");
                var ent = test[entry++];
                //var ent = test[int.Parse(Console.ReadLine())];
                var id = ent.GetProperty("contentId").GetString();
                var start = DateTime.ParseExact(id.Substring(0, 14), "yyyyMMddHHmmss", null);
                var end = DateTime.ParseExact(id.Split('$')[1].Substring(0, 14), "yyyyMMddHHmmss", null);
                Console.WriteLine($"{start} - {end}");
                var url = ent.GetProperty("contentUri").GetString();

                var records = await GetContentItem(url);
                allRecords.AddRange(records);
                foreach (var group in records.GroupBy(r => r.Operation))
                {
                    Console.WriteLine($"\t{group.Key}: {group.Count()}");
                    if (group.Key == "TeamSettingChanged" || group.Any(r => r.UserId == "g.oomens@uva.nl"))
                        Console.WriteLine("??");
                }
            }

            Console.WriteLine();
            foreach (var group in allRecords.GroupBy(r => r.Operation))
            {
                Console.WriteLine($"\t{group.Key}: {group.Count()}");
                if (group.Key == "TeamSettingChanged")
                    Console.WriteLine("??");
            }
        }

        public async Task<AuditRecord[]> GetContentItem(string uri)
        {
            var res = await Client.GetStringAsync(uri);
            var records = JsonSerializer.Deserialize<AuditRecord[]>(res);
            return records;
        }

        public class AuditRecord
        {
            public int RecordType { get; set; }
            public DateTime CreationTime { get; set; }
            public string Operation { get; set; }
            public string OperationDetails { get; set; }
            public string UserId { get; set; }
            public string ObjectId { get; set; }
            public string Workload { get; set; }
            public string AADGroupId { get; set; }
            public Member[] Members { get; set; }

            public string NewValue { get; set; }
            public string Name { get; set; }
            public string TeamGuid { get; set; }

            public override string ToString()
                => $"{Workload}: {Operation}";
        }

        public class Member
        {
            public string UPN { get; set; }
        }
    }
}
