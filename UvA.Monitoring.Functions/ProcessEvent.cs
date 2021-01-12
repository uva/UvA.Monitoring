using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using UvA.Monitoring.Shared;
using System.Linq;

namespace UvA.Monitoring.Functions
{
    public static class ProcessEvent
    {
        [FunctionName("ProcessEvent")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JToken.Parse(requestBody);
            if (payload is JArray data)
            {
                var conf = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();
                var tenants = new[] { conf.GetSection("UvA"), conf.GetSection("HvA") };
                log.LogInformation($"Received {data.Count} blocks");
                var tenantId = (string)data[0]["tenantId"];
                var tenant = tenants.First(t => t["tenantId"] == tenantId);
                var checker = new StreamChecker(log, tenant);
                await checker.Connect();
                foreach (var el in payload)
                    await checker.Check(el["contentUri"].ToString());
            }
            else
                log.LogInformation("Received message without array");

            return new OkResult();
        }
    }
}
