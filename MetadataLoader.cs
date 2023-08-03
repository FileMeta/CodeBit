using Bredd;
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

        /// <summary>
        /// Read CodeBit Metadata from multiple sources
        /// </summary>
        /// <param name="target">The target to be read</param>
        /// <param name="targetType">Target type (filename, url, name (in directory)</param>
        /// <param name="version">Version (only relevant for <see cref="TargetType.CodebitName"/></param>
        /// <returns>Codebit Metadata.</returns>
        /// <exception cref="ArgumentException">Target type is not Filename, CodebitUrl, or CodebitName</exception>
        /// <exception cref="ApplicationException">Target was not found.</exception>
        public static CodeBitMetadata Read(string target, TargetType targetType, SemVer? version = null)
        {
            switch (targetType)
            {
                case TargetType.Filename:
                    {
                        var metadata = ReadCodeBitFromFile(target);
                        if (metadata is null) throw new ApplicationException($"Codebit file '{target}' not found.");
                        return metadata;
                    }

                case TargetType.CodebitUrl:
                    {
                        var metadata = ReadCodeBitFromUrl(target);
                        if (metadata is null) throw new ApplicationException($"Codebit at URL '{target}' not found.");
                        return metadata;
                    }

                case TargetType.CodebitName:
                    {
                        var domainName = MetadataLoader.GetCodebitDomainName(target);
                        var dirUrl = GetDirectoryUrl(domainName);
                        if (dirUrl is null) throw new ApplicationException($"Domain '{domainName}' does not have a directory.");
                        var reader = GetDirectoryFromUrl(dirUrl);
                        if (reader == null) throw new ApplicationException($"Unable to read directory for '{domainName}' from URL '{dirUrl}'.");
                        var metadata = reader.Find(target, version ?? SemVer.Max);
                        if (metadata is null) throw new ApplicationException((version is null)
                            ? $"Unable to find a directory entry for '{target}'."
                            : $"Unable to find a directory entry for '{target}' version '{version}'.");
                        return metadata;
                    }

                case TargetType.DirectoryDomain:
                    throw new ArgumentException($"Cannot specify a directory domain for this operation.");

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
