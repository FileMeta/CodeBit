using Bredd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBit
{
    internal static class MetadataLoader
    {
        public static CodeBitMetadata ReadCodeBitFromFile(string filename)
        {
            using (var reader = new StreamReader(filename, Encoding.UTF8, true))
            {
                return CodeBitMetadata.Read(reader);
            }
        }

        public static CodeBitMetadata ReadCodeBitFromUrl(string url)
        {
            using (var reader = new StreamReader(Http.Get(url), Encoding.UTF8, true, 512, false))
            {
                return CodeBitMetadata.Read(reader);
            }
        }

        public static CodeBitMetadata Read(string urlOrFilename)
        {
            return (urlOrFilename.StartsWith("http://") || urlOrFilename.StartsWith("https://"))
                ? ReadCodeBitFromUrl(urlOrFilename)
                : ReadCodeBitFromFile(urlOrFilename);
        }

        public static string GetCodebitDomainName(string codebitName)
        {
            int slash = codebitName.IndexOf('/');
            if (slash < 0) throw new ArgumentException($"Codebit name '{codebitName}' does not have a slash separating domain name from codebit name.");
            return codebitName.Substring(0, slash);
        }

        public static string? GetDirectoryUrl(string domainName)
        {
            string? dirRecord = null;
            var txtRecords = WinDnsQuery.GetTxtRecords("_dir." + domainName);
            if (txtRecords != null)
            {
                foreach (var txtRecord in txtRecords)
                {
                    if (txtRecord.StartsWith("dir="))
                        return txtRecord.Substring(4).Trim();
                }
            }
            return null;
        }

        public static DirectoryReader GetDirectoryFromUrl(string url)
        {
            return new DirectoryReader(Http.Get(url));
        }
    }
}
