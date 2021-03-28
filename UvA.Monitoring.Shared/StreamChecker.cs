using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace UvA.Monitoring.Shared
{
    public class StreamChecker
    {
        ILogger Log;
        ManagementConnector Connector;
        HttpClient Client;

        string MemberReportUrl;
        string SettingReportUrl;
        string StreamReportUrl;
        string TenantId;

        public StreamChecker(ILogger log, IConfiguration config, string tenant)
        {
            TenantId = tenant;
            Log = log;
            Connector = new ManagementConnector(config);
            Client = new HttpClient();

            MemberReportUrl = config["ReportUrl"];
            SettingReportUrl = config["SettingReportUrl"];
            StreamReportUrl = config["StreamReportUrl"];
        }

        public async Task Connect()
        {
            await Connector.Connect();
        }

        public async Task Check(string uri)
        {
            var res = await Connector.GetContentItem(uri);
            Log.LogInformation($"Getting block {uri} ({res.Length} records)");
            var stream = res.Where(r => r.Workload == "MicrosoftStream");
            foreach (var ev in stream)
            {
                var obj = string.IsNullOrWhiteSpace(ev.OperationDetails) ? null : JObject.Parse(ev.OperationDetails);
                Log.LogInformation($"{ev.UserId} {ev.Operation}: {ev.OperationDetails}");
                if (ev.Operation == "StreamEditVideo")
                {
                    var before = (string)obj["Before"]["PrivacyMode"];
                    var after = (string)obj["After"]["PrivacyMode"];
                    if (before != after && after == "organization")
                        await Client.PostJsonAsync(StreamReportUrl, new { ev.UserId });
                }
                if (ev.Operation == "StreamInvokeVideoUpload" && (string)obj["PrivacyMode"] == "organization")
                    await Client.PostJsonAsync(StreamReportUrl, new { ev.UserId });
            }

            var memberAdd = res.Where(r => r.Operation == "MemberAdded" && !string.IsNullOrEmpty(r.AADGroupId) && r.Members != null);
            foreach (var ev in memberAdd)
                foreach (var mem in ev.Members)
                    await Client.PostJsonAsync(MemberReportUrl, new
                    {
                        UserId = mem.UPN,
                        GroupId = ev.AADGroupId,
                        TenantId = TenantId,
                        ev.CreationTime
                    });


            foreach (var ev in res.Where(r => r.Operation == "TeamSettingChanged" && r.NewValue == "public" && r.Name == "Team access type"))
            {
                Log.LogInformation($"Public team: {ev.TeamGuid} by {ev.UserId}");
                await Client.PostJsonAsync(SettingReportUrl, new
                {
                    TeamId = ev.TeamGuid,
                    UserId = ev.UserId,
                    TenantId = TenantId,
                    ev.CreationTime
                });
            }
        }
    }
}
