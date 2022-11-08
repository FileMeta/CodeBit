using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;

namespace CodeBit
{
    internal static class Http
    {
        static HttpClient s_client;
        
        static Http()
        {
            s_client = new HttpClient();
            s_client.DefaultRequestHeaders.UserAgent.ParseAdd("CodeBit/1.0 (CodeBit CLI Tool)");
            s_client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        }

        public static Stream Get(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            HttpResponseMessage response;
            try
            {
                response = s_client.SendAsync(request).GetAwaiter().GetResult();
            }
            catch (HttpRequestException err)
            {
                if (err.InnerException != null) throw err.InnerException;
                throw;
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP Error: {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            return response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        }

        public static String GetString(string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = s_client.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"HTTP Error: {response.StatusCode} {response.ReasonPhrase}");
            }
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

    }
}
