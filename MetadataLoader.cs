﻿using Bredd;
using FileMeta;
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
        /// <summary>
        /// Load CodeBit Metadata from a file
        /// </summary>
        /// <param name="filename">The Filename</param>
        /// <returns>Metadaata or Null if file not found.</returns>
        public static CodeBitMetadata? ReadCodeBitFromFile(string filename)
        {
            try
            {
                using (var reader = new StreamReader(filename, Encoding.UTF8, true))
                {
                    var metadata = CodeBitMetadata.Read(reader);
                    metadata.FilenameForValidation = filename;
                    return metadata;
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public static CodeBitMetadata? ReadCodeBitFromUrl(string url)
        {
            try
            {
                using (var reader = new StreamReader(Http.Get(url), Encoding.UTF8, true, 512, false))
                {
                    return CodeBitMetadata.Read(reader);
                }
            }
            catch (HttpRequestException err)
            {
                if (err.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                throw;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return null; // Host not found.
            }
        }

        public static bool IsHttpUrl(string urlOrFilename)
        {
            return urlOrFilename.StartsWith("http://") || urlOrFilename.StartsWith("https://");
        }

        public static CodeBitMetadata? Read(string urlOrFilename, TargetType targetType, SemVer? version = null)
        {
            switch (targetType)
            {
                case TargetType.Filename:
                    return ReadCodeBitFromFile(urlOrFilename);

                case TargetType.CodebitUrl:
                    return ReadCodeBitFromUrl(urlOrFilename);

                case TargetType.CodebitName:
                    throw new NotImplementedException();

                default:
                    throw new ArgumentException($"Unexpected value '{targetType}'.", nameof(targetType));
            }
        }

        public static string GetCodebitDomainName(string codebitName)
        {
            int slash = codebitName.IndexOf('/');
            if (slash < 0) throw new ArgumentException($"Codebit name '{codebitName}' does not have a slash separating domain name from codebit name.");
            return codebitName.Substring(0, slash);
        }

        public static string? GetDirectoryUrl(string domainName)
        {
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

        public static DirectoryReader? GetDirectoryFromUrl(string url)
        {
            try
            {
                return new DirectoryReader(Http.Get(url));
            }
            catch (HttpRequestException err)
            {
                if (err.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
                throw;
            }
            catch (System.Net.Sockets.SocketException)
            {
                return null; // Host not found.
            }

        }
    }
}
