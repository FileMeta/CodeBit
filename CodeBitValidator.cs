//#define RAW
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Text;
using Bredd;
using FileMeta;

namespace CodeBit
{
    internal static class CodeBitValidator
    {
        public static void ValidateFile(string filename)
        {
            (var fileValidationLevel, var fileMetadata) = ValidateAndReport(filename, "Metadata");
            if (fileMetadata is null) return;

            Console.Write(fileMetadata.ToString());
            Console.WriteLine();
            if (fileValidationLevel > ValidationLevel.FailRecommended) return;

            (var pubValidationLevel, var pubMetadata) = ValidateAndReport(fileMetadata.Url, "Published Copy");

            if (pubMetadata is not null && pubValidationLevel <= ValidationLevel.FailRecommended)
            {
                Console.WriteLine("Comparing File with Published Copy...");
                CompareAndReport(fileMetadata, pubMetadata, "File", "Published", true);
            }

            CompareWithDirectoryAndReport(fileMetadata, "File");
        }

        public static void ValidatePublishedCodebit(string url)
        {
            (var fileValidationLevel, var metadata) = ValidateAndReport(url, "Published Metadata");
            if (metadata is null) return;

            Console.Write(metadata.ToString());
            Console.WriteLine();
            if (fileValidationLevel > ValidationLevel.FailRecommended) return;

            CompareWithDirectoryAndReport(metadata, "Published Metadata");
        }

        public static void ValidateByName(string codebitName, SemVer? version = null)
        {
            if (version is null)
            {
                Console.WriteLine($"Validating CodeBit '{codebitName}'...");
                version = SemVer.Max;
            }
            else
            {
                Console.WriteLine($"Validating CodeBit '{codebitName}' v{version}...");
            }

            var dirMetadata = FindInDirectoryOrReport(codebitName, version);
            if (dirMetadata is null) return;

            if (string.IsNullOrWhiteSpace(dirMetadata.Url))
            {
                Console.WriteLine("Invalid directory entry. No URL specified.");
                return;
            }

            (var pubValidationLevel, var pubMetadata) = ValidateAndReport(dirMetadata.Url, "Published Copy");
            if (pubMetadata is null) return;

            Console.Write(pubMetadata.ToString());
            Console.WriteLine();
            if (pubValidationLevel > ValidationLevel.FailRecommended) return;

            Console.WriteLine("Comparing with directory entry...");
            CompareAndReport(pubMetadata, dirMetadata, "Published", "Directory");

            if (pubMetadata is null || pubValidationLevel > ValidationLevel.FailRecommended) return;
        }

        public static void ValidateDirectory(string domainName)
        {
            var dirUrl = MetadataLoader.GetDirectoryUrl(domainName);
            if (dirUrl is null)
            {
                Console.WriteLine($"No dir TXT record found on domain '{domainName}'.");
                Console.WriteLine($"DNS must include a TXT record on the domain '_dir.{domainName}' that contains\nthe URL, 'dir=<url of the directory>'.");
                return;
            }

            Console.WriteLine($"DNS Success: Directory for '{domainName}' is located at '{dirUrl}'.");

            DirectoryReader reader;
            reader = MetadataLoader.GetDirectoryFromUrl(dirUrl);
            if (reader is null)
            {
                Console.WriteLine("Directory not found.");
                return;
            }

            using (reader)
            {
                var dirMetadata = reader.ReadDirectory();

                ValidationLevel validationLevel;
                string validationDetail;
                (validationLevel, validationDetail) = dirMetadata.Validate();

                if (validationLevel == ValidationLevel.Pass)
                {
                    Console.WriteLine("Directory global metadata passes validation.");
                }
                else if (validationLevel == ValidationLevel.FailRecommended)
                {
                    Console.WriteLine("Warning: Directory global metadata fails one or more recommended but optional requirements:");
                    Console.WriteLine(validationDetail);
                }
                else
                {
                    Console.WriteLine("Directory global metadata fails one or more mandatory requirements:");
                    Console.WriteLine(validationDetail);
                }
                Console.WriteLine();

                int nCodeBits = 0;
                int nFailToValidate = 0;
                int nFailToCompare = 0;
                int nSourceCode = 0;
                int nOther = 0;
                for (; ; )
                {
                    var codebitMetadata = reader.ReadCodeBit();
                    if (codebitMetadata == null) break;
                    Console.WriteLine($"=== Directory Entry: {(codebitMetadata.Name ?? "(Unknown)")}");

                    if (codebitMetadata.IsCodeBit)
                    {
                        nCodeBits++;
                        validationLevel = ValidateAndReport(codebitMetadata);

                        if (validationLevel <= ValidationLevel.FailRecommended)
                        {
                            (var pubValidationLevel, var pubMetadata) = ValidateAndReport(codebitMetadata.Url, "Published Codebit");
                            if (pubValidationLevel <= ValidationLevel.FailRecommended)
                            {
                                if (ValidationLevel.FailRecommended < CompareAndReport(codebitMetadata, pubMetadata, "Directory", "Published"))
                                {
                                    ++nFailToCompare;
                                }
                            }
                            else
                            {
                                ++nFailToValidate;
                            }
                        }
                        else
                        {
                            ++nFailToValidate;
                        }
                    }
                    else if (codebitMetadata.IsSoftwareSourceCode)
                    {
                        Console.WriteLine("Non-Codebit Source Code.");
                        Console.WriteLine();
                        ++nSourceCode;
                    }
                    else
                    {
                        Console.WriteLine("Not Source Code.");
                        Console.WriteLine();
                        ++nOther;
                    }

                }

                Console.WriteLine($"{nCodeBits} CodeBits in the directory.");
                if (nFailToValidate > 0) Console.WriteLine($"{nFailToValidate} CodeBits failed validation.");
                if (nFailToCompare > 0) Console.WriteLine($"{nFailToCompare} CodeBits failed comparison.");
                if (nSourceCode > 0) Console.WriteLine($"{nSourceCode} Non-CodeBit source code entries in the directory.");
                if (nOther > 0) Console.WriteLine($"{nOther} other entries in the directory.");
            }
        }

        private static (ValidationLevel validationLevel, CodeBitMetadata? metadata) ValidateAndReport(string filenameOrUrl, string label)
        {
            Console.WriteLine($"Validating {label} in '{filenameOrUrl}'...");
            var metadata = MetadataLoader.Read(filenameOrUrl);
            if (metadata == null)
            {
                Console.WriteLine($"CodeBit '{filenameOrUrl}' not found.");
                Console.WriteLine();
                return (ValidationLevel.Fail, null);
            }
            return (ValidateAndReport(metadata), metadata);
        }

        private static ValidationLevel ValidateAndReport(CodeBitMetadata metadata)
        {
            (var validationLevel, var validationDetail) = metadata.Validate();

            if (validationLevel == ValidationLevel.Pass)
            {
                Console.WriteLine("CodeBit metadata passes validation.");
            }
            else if (validationLevel == ValidationLevel.FailRecommended)
            {
                Console.WriteLine("Warning: CodeBit fails one or more recommended but optional requirements:");
                Console.Out.WriteLineIndented(3, validationDetail);
            }
            else
            {
                Console.WriteLine("CodeBit fails one or more mandatory requirements:");
                Console.Out.WriteLineIndented(3, validationDetail);
            }
            Console.WriteLine();
            return validationLevel;
        }

        private static ValidationLevel CompareAndReport(CodeBitMetadata a, CodeBitMetadata b, string aLabel, string bLabel, bool expectUrlMatch = false)
        {
            (ValidationLevel cmpValidationLevel, string cmpValidationDetail) = a.CompareTo(b, aLabel, bLabel, expectUrlMatch);

            if (cmpValidationLevel == ValidationLevel.Pass)
            {
                Console.WriteLine($"{aLabel} and {bLabel} CodeBits match.");
            }
            else if (cmpValidationLevel == ValidationLevel.FailRecommended)
            {
                Console.WriteLine($"{aLabel} and {bLabel} CodeBits fail one or more recommended but optional comparisons:");
                Console.Out.WriteLineIndented(3, cmpValidationDetail);
            }
            else
            {
                Console.WriteLine($"{aLabel} and {bLabel} CodeBits fail one or more required comparisons:");
                Console.Out.WriteLineIndented(3, cmpValidationDetail);
            }
            Console.WriteLine();
            return cmpValidationLevel;
        }

        private static void CompareWithDirectoryAndReport(CodeBitMetadata a, string aLabel)
        {
            Console.WriteLine($"Validating directory entry for '{a.Name}' v{a.Version}...");

            var dirMetadata = FindInDirectoryOrReport(a.Name, a.Version);
            if (dirMetadata is null) return;

            var validationLevel = ValidateAndReport(dirMetadata);
            if (validationLevel > ValidationLevel.FailRecommended) return;

            Console.WriteLine($"Comparing {aLabel} with directory...");
            CompareAndReport(a, dirMetadata, aLabel, "Directory");
        }

        static CodeBitMetadata? FindInDirectoryOrReport(string codeBitName, SemVer version)
        {
            string domainName = MetadataLoader.GetCodebitDomainName(codeBitName);
            var dirUrl = MetadataLoader.GetDirectoryUrl(domainName);
            if (dirUrl is null)
            {
                Console.WriteLine($"No DNS directory entry for domain '{domainName}'.");
                Console.WriteLine();
                return null;
            }
            var reader = MetadataLoader.GetDirectoryFromUrl(dirUrl);
            if (reader == null)
            {
                Console.WriteLine($"Unable to read directory for domain '{domainName} from URL '{dirUrl}'.");
                Console.WriteLine();
                return null;
            }

            CodeBitMetadata? dirMetadata = null;
            for (; ; )
            {
                var candidate = reader.ReadCodeBit();
                if (candidate is null) break;
                if (string.Equals(candidate.Name, codeBitName, StringComparison.Ordinal)
                    && (dirMetadata is null || candidate.Version.CompareTo(dirMetadata.Version) > 0)
                    && candidate.Version.CompareTo(version) <= 0)
                {
                    dirMetadata = candidate;
                }
            }

            if (dirMetadata is null)
            {
                Console.WriteLine($"Codebit '{codeBitName}' v{version} is not listed in the directory.");
                Console.WriteLine();
            }

            return dirMetadata;
        }

    } // Class CodeBitValidator

    static class IndentedWrite
    {
        public static void WriteLineIndented(this TextWriter writer, int indentation, String str)
        {
            var indent = new String(' ', indentation);
            var s = str.AsSpan();
            var len = s.Length;
            for (int i = 0; i < s.Length;)
            {
                writer.Write(indent);

                // Slice and write one line
                int a = i;
                while (i < len && s[i] != '\n') ++i;
                var e = i;
                while (e > a && s[e - 1] == '\r') --e;
                ++i;
                while (i < len && s[i] == '\r') ++i;
                writer.Write(s[a..e]);
                writer.WriteLine();
            }
        }
    }
}
 