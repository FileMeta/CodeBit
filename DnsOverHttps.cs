using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CodeBit
{
    internal static class DnsOverHttps {
        const string c_acceptType = "application/dns-json";
        const string c_defaultDnsHost = "cloudflare-dns.com";
        static string s_dnsHost = c_defaultDnsHost;

        static public string DnsHost { get { return s_dnsHost; } set { s_dnsHost = value; } }

        public static string[] GetTxtRecords(string domainName) {
            string url = $"https://{DnsHost}/dns-query?name={HttpUtility.UrlEncode(domainName)}&type=TXT";
            var reader = JsonXmlReader.Create(Http.Get(url, "DNS TXT", c_acceptType));

            reader.Read();
            if (reader.NodeType != JsonNodeType.StartObject)
                throw new ApplicationException("Invalid DNS JSON Response File Format.");
            reader.Read();

            // Skip until "Answer" is found.
            do {
                if (reader.NodeType == JsonNodeType.StartArray && reader.Name == "Answer") break;
            } while (reader.Read());
            reader.Read();

            // Read each answer
            List<string> result = new List<string>();
            do {
                if (reader.NodeType != JsonNodeType.StartObject) break;
                reader.Read();
                do {
                    if (reader.NodeType == JsonNodeType.EndElement) break;
                    if (reader.NodeType == JsonNodeType.Value && reader.Name == "data") {
                        string val = reader.Value;
                        if (val[0] == '"' && val[val.Length - 1] == '"') { // Not sure why there are embedded quotes but take care of them here.
                            val = val.Substring(1, val.Length - 2);
                        }
                        result.Add(val);
                    }
                } while (reader.Skip());
            } while (reader.Skip());

            return result.ToArray();
        }
    }
}
