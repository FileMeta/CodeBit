using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Bredd;

namespace CodeBit
{
    internal static class CodeBitValidator
    {
        public static void ValidateFile(string filename)
        {
            var path = Path.GetFullPath(filename);
            Console.WriteLine($"Validating CodeBit metadata in '{path}'...");

            CodeBitMetadata metadata;
            ValidationLevel validationLevel;
            string validationDetail;

            try
            {
                using (var reader = new StreamReader(path, Encoding.UTF8, true))
                {
                    metadata = CodeBitMetadata.Read(reader);
                    (validationLevel, validationDetail) = metadata.Validate();
                }

                if (validationLevel == ValidationLevel.Pass)
                {
                    Console.WriteLine("CodeBit metadata passes validation.");
                }
                else if (validationLevel == ValidationLevel.FailRecommended)
                {
                    Console.WriteLine("Warning: CodeBit fails one or more recommended but optional requirements:");
                    Console.WriteLine(validationDetail);
                }
                else
                {
                    Console.WriteLine("CodeBit fails one or more mandatory requirements:");
                    Console.WriteLine(validationDetail);
                }

                Console.WriteLine();
                Console.Write(metadata.ToString());
                Console.WriteLine();
                if (validationLevel > ValidationLevel.FailRecommended) return;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Failed to read and parse CodeBit: {err.Message}");
                return;
            }

            Console.WriteLine($"Validating published copy at '{metadata.Url}'...");

            CodeBitMetadata pubMetadata;
            ValidationLevel pubValidationLevel;
            string pubValidationDetail;

            try
            {
                using (var reader = new StreamReader(Http.Get(metadata.Url), Encoding.UTF8, true, 512, false))
                {
                    pubMetadata = CodeBitMetadata.Read(reader);
                    (pubValidationLevel, pubValidationDetail) = pubMetadata.Validate();
                }

                if (pubValidationLevel == ValidationLevel.Pass)
                {
                    Console.WriteLine("Published CodeBit metadata passes validation.");
                }
                else if (pubValidationLevel == ValidationLevel.FailRecommended)
                {
                    Console.WriteLine("Warning: Published CodeBit fails one or more recommended but optional requirements:");
                    Console.Write(pubValidationDetail);
                }
                else
                {
                    Console.WriteLine("Published CodeBit fails one or more mandatory requirements:");
                    Console.Write(pubValidationDetail);
                }

                if (validationLevel > ValidationLevel.FailRecommended) return;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Failed to read and parse published CodeBit: {err.Message}");
                return;
            }

            int cmp = metadata.Version.CompareTo(pubMetadata.Version);
            if (cmp < 0)
            {
                Console.WriteLine($"Local version ({metadata.Version}) is older than the published version ({pubMetadata.Version}). Consider updating.");
                return;
            }
            if (cmp > 0)
            {
                Console.WriteLine($"Local version ({metadata.Version}) is newer than the published version ({pubMetadata.Version}). Are you preparing an update?");
                return;
            }

        }

        public static void ValidateDirectory(string domainName)
        {
            string dirRecord = null;
            var txtRecords = WinDnsQuery.GetTxtRecords("_dir." + domainName);
            if (txtRecords != null)
            {
                foreach(var txtRecord in txtRecords)
                {
                    if (txtRecord.StartsWith("dir="))
                    {
                        dirRecord = txtRecord;
                        break;
                    }
                }
            }
            if (dirRecord == null)
            {
                Console.WriteLine($"No dir TXT record found on domain '{domainName}'.");
                Console.WriteLine($"DNS must include a TXT record on the domain '_dir.{domainName}' that contains\nthe URL, 'dir=<url of the directory>'.");
                return;
            }

            var dirUrl = dirRecord.Substring(4).Trim();
            Console.WriteLine($"DNS Success: Directory for '{domainName}' is located at '{dirUrl}'.");

            Stream stream;
            try
            {
                stream = Http.Get(dirUrl);
            }
            catch (Exception err)
            {
                Console.WriteLine("Failed to read directory: " + err.Message);
                return;
            }

            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                Console.WriteLine(reader.ReadToEnd());
            }
        }

        /*
        static (bool match, string detail) CompareMetadata(CodeBitMetadata a, CodeBitMetadata b)
        {
            if (!string.Equals(a.Version, b.Version))
        }
        */
    }
}
 