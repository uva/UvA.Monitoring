using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace UvA.Monitoring.TestTool
{
    class LogTool
    {
        public static IConfiguration GetConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public static ILogger<T> CreateLogger<T>()
        {
            var sp = new ServiceCollection().AddLogging(b => b.AddConsole()).BuildServiceProvider();
            return sp.GetService<ILoggerFactory>().CreateLogger<T>();
        }
    }
}
