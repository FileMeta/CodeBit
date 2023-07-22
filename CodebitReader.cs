using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBit
{
    internal static class CodebitReader
    {
        public static CodeBitMetadata ReadFromFile(string filename)
        {
            using (var reader = new StreamReader(filename, Encoding.UTF8, true))
            {
                return CodeBitMetadata.Read(reader);
            }
        }

        public static CodeBitMetadata ReadFromUrl(string url)
        {
            using (var reader = new StreamReader(Http.Get(url), Encoding.UTF8, true, 512, false))
            {
                return CodeBitMetadata.Read(reader);
            }
        }

        public static CodeBitMetadata Read(string urlOrFilename)
        {
            return (urlOrFilename.StartsWith("http://") || urlOrFilename.StartsWith("https://"))
                ? ReadFromUrl(urlOrFilename)
                : ReadFromFile(urlOrFilename);
        }
    }
}
