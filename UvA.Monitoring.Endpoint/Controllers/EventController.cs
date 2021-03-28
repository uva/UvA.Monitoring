using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UvA.Monitoring.Shared;

namespace UvA.Monitoring.Endpoint.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventController : ControllerBase
    {
        private readonly ILogger<EventController> _logger;
        private readonly IConfiguration _conf;

        public EventController(ILogger<EventController> logger, IConfiguration conf)
        {
            _logger = logger;
            _conf = conf;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement payload)
        {
            if (payload.ValueKind == JsonValueKind.Array)
            {
                var tenants = new[] { _conf.GetSection("UvA"), _conf.GetSection("HvA") };
                var tenantId = payload[0].GetProperty("tenantId").GetString();
                _logger.LogInformation($"Received {payload.GetArrayLength()} blocks (tenant {tenantId})");
                var tenant = tenants.First(t => t["tenantId"] == tenantId);
                var checker = new StreamChecker(_logger, tenant, tenantId);
                await checker.Connect();
                foreach (var el in payload.EnumerateArray())
                    await checker.Check(el.GetProperty("contentUri").GetString());
            }
            else
                _logger.LogInformation("Received message without array");

            return Ok();
        }
    }
}
