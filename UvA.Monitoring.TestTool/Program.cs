using System;
using UvA.Monitoring.Shared;

namespace UvA.Monitoring.TestTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var conn = new ManagementConnector(LogTool.GetConfig().GetSection("HvA"));
            conn.Connect().Wait();
            //conn.CreateSubscription("https://streammonitoring.azurewebsites.net/api/ProcessEvent").Wait();
            conn.GetContent().Wait();
        }
    }
}
