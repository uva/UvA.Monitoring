using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UvA.Monitoring.Shared
{
    public static class HttpExtensions
    {
        public static async Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string requestUri, T obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return await client.PostAsync(requestUri, new StringContent(json, Encoding.UTF8, "application/json"));
        }
    }
}
