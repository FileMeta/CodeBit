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

        /// <summary>
        /// Retrieve a URL in the form of a stream. Upon errors throw an ApplicationException
        /// with a user-friendly error message.
        /// </summary>
        /// <param name="url">The URL to retrieve</param>
        /// <param name="resourceType">The type of resource being retrieved - for error reporting.</param>
        /// <returns>An open stream containing the response.</returns>
        /// <exception cref="ApplicationException">An exception with a user-friendly error message.</exception>
        public static Stream Get(string url, string resourceType, string? accept = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(accept)) {
                request.Headers.Add("Accept", accept);
            }
            HttpResponseMessage response;
            try
            {
                response = s_client.SendAsync(request).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    var detail = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!string.Equals(detail, $"{(int)response.StatusCode}: {response.ReasonPhrase}"))
                        detail = $"{(int)response.StatusCode}: {response.ReasonPhrase}\r\n{detail}";
                    throw new ApplicationException($"Failed to read {resourceType} from {url}.\r\n{detail}");
                }
                return response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            }
            catch (HttpRequestException err) {
                throw new ApplicationException($"Failed to read {resourceType} from {url}.\r\n{err.Message}");
            }
            catch (System.Net.Sockets.SocketException)
            {
                throw new ApplicationException($"Failed to read {resourceType} from {url}.\r\nHost not found.");
            }
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
