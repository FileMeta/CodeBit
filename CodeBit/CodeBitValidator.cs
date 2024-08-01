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
        public static int ValidateFile(string filename)
        {
            (var fileValidationLevel, var fileMetadata) = ValidateFileAndReport(filename);

            Console.Write(fileMetadata.ToString());
            Console.WriteLine();
            if (fileValidationLevel > ValidationLevel.FailRecommended) return -1;

            (var pubValidationLevel, var pubMetadata) = ValidateUrlAndReport(fileMetadata.Url, "Published Copy");

            if (pubMetadata is not null && pubValidationLevel <= ValidationLevel.FailRecommended)
            {
                Console.WriteLine("Comparing File with Published Copy...");
                CompareAndReport(fileMetadata, pubMetadata, "File", "Published", true);
            }

            return CompareWithDirectoryAndReport(fileMetadata, "File");
        }

        public static int ValidatePublishedCodebit(string url)
        {
            (var fileValidationLevel, var metadata) = ValidateUrlAndReport(url, "Published Metadata");
            if (metadata is null) return -1;

            Console.Write(metadata.ToString());
            Console.WriteLine();
            if (fileValidationLevel > ValidationLevel.FailRecommended) return -1;

            return CompareWithDirectoryAndReport(metadata, "Published Metadata");
        }

        public static int ValidateByName(string codebitName, SemVer? version = null)
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
            if (dirMetadata is null) return -1;

            if (string.IsNullOrWhiteSpace(dirMetadata.Url))
            {
                Console.WriteLine("Invalid directory entry. No URL specified.");
                return -1;
            }

            (var pubValidationLevel, var pubMetadata) = ValidateUrlAndReport(dirMetadata.Url, "Published Copy");
            if (pubMetadata is null) return -1;

            Console.Write(pubMetadata.ToString());
            Console.WriteLine();
            if (pubValidationLevel > ValidationLevel.FailRecommended) return -1;

            Console.WriteLine("Comparing with directory entry...");
            CompareAndReport(pubMetadata, dirMetadata, "Published", "Directory");

            if (pubMetadata is null || pubValidationLevel > ValidationLevel.FailRecommended) return -1;

            return 0;
        }

        public static int ValidateDirectory(string domainName)
        {
            var dirUrl = MetadataLoader.GetDirectoryUrl(domainName);
            if (dirUrl is null)
            {
                Console.WriteLine($"No dir TXT record found on domain '{domainName}'.");
                Console.WriteLine($"DNS must include a TXT record on the domain '_dir.{domainName}' that contains\n  'dir=<url of the directory>'.");
                return -1;
            }

            Console.WriteLine($"DNS Success: Directory for '{domainName}' is located at '{dirUrl}'.");

            DirectoryReader? reader;
            reader = MetadataLoader.GetDirectoryFromUrl(dirUrl);
            if (reader is null)
            {
                Console.WriteLine("Directory not found.");
                return -1;
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
                int nValidateFailures = 0;
                int nValidateWarnings = 0;
                int nCompareFailures = 0;
                int nCompareWarnings = 0;
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
                        validationLevel = ValidateAndReport("Directory Entry", codebitMetadata);

                        if (validationLevel <= ValidationLevel.FailRecommended)
                        {
                            if (validationLevel == ValidationLevel.FailRecommended)
                                nValidateWarnings++;

                            (var pubValidationLevel, var pubMetadata) = ValidateUrlAndReport(codebitMetadata.Url, "Published Codebit");
                            if (pubValidationLevel <= ValidationLevel.FailRecommended)
                            {
                                if (pubValidationLevel == ValidationLevel.FailRecommended)
                                    nValidateWarnings++;

                                var cmpValidationLevel = CompareAndReport(codebitMetadata, pubMetadata, "Directory", "Published");
                                if (cmpValidationLevel > ValidationLevel.FailRecommended) {
                                    ++nCompareFailures;
                                }
                                else if (cmpValidationLevel == ValidationLevel.FailRecommended) {
                                    ++nCompareWarnings;
                                }
                            }
                            else
                            {
                                ++nValidateFailures;
                            }
                        }
                        else
                        {
                            ++nValidateFailures;
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
                if (nValidateFailures > 0) Console.WriteLine($"{nValidateFailures} CodeBits failed validation.");
                if (nValidateWarnings > 0) Console.WriteLine($"{nValidateWarnings} CodeBits with validation warnings.");
                if (nCompareFailures > 0) Console.WriteLine($"{nCompareFailures} CodeBits failed comparison.");
                if (nCompareWarnings > 0) Console.WriteLine($"{nCompareWarnings} CodeBits with comparison warnings.");
                if (nSourceCode > 0) Console.WriteLine($"{nSourceCode} Non-CodeBit source code entries in the directory.");
                if (nOther > 0) Console.WriteLine($"{nOther} Non-Source Code entries in the directory.");
                return nValidateFailures + nCompareFailures > 0 ? -1 : 0;
            }
        }

        private static (ValidationLevel validationLevel, CodeBitMetadata metadata) ValidateFileAndReport(string filename)
        {
            Console.WriteLine($"Validating Metadata in '{filename}'...");
            var metadata = MetadataLoader.ReadCodeBitFromFile(filename);
            return (ValidateAndReport("Local CodeBit", metadata), metadata);
        }

        private static (ValidationLevel validationLevel, CodeBitMetadata metadata) ValidateUrlAndReport(string url, string label)
        {
            Console.WriteLine($"Validating {label} in '{url}'...");
            var metadata = MetadataLoader.ReadCodeBitFromUrl(url);
            return (ValidateAndReport("Published CodeBit", metadata), metadata);
        }

        private static ValidationLevel ValidateAndReport(string reference, CodeBitMetadata metadata)
        {
            (var validationLevel, var validationDetail) = metadata.Validate();

            if (validationLevel == ValidationLevel.Pass)
            {
                Console.WriteLine($"{reference} metadata passes validation.");
            }
            else if (validationLevel == ValidationLevel.FailRecommended)
            {
                Console.WriteLine($"Warning: {reference} fails one or more recommended but optional requirements:");
                Console.Out.WriteLineIndented(3, validationDetail);
            }
            else
            {
                Console.WriteLine($"Error: {reference} fails one or more mandatory requirements:");
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

        private static int CompareWithDirectoryAndReport(CodeBitMetadata a, string aLabel)
        {
            Console.WriteLine($"Validating directory entry for '{a.Name}' v{a.Version}...");

            var dirMetadata = FindInDirectoryOrReport(a.Name, a.Version);
            if (dirMetadata is null) return -1;

            var validationLevel = ValidateAndReport("Directory Entry", dirMetadata);
            if (validationLevel > ValidationLevel.FailRecommended) return -1;

            Console.WriteLine($"Comparing {aLabel} with directory...");
            return CompareAndReport(a, dirMetadata, aLabel, "Directory") >= ValidationLevel.FailMandatory ? -1 : 0;
        }

        static public CodeBitMetadata? FindInDirectoryOrReport(string codeBitName, SemVer version)
        {
            string domainName = MetadataLoader.GetCodebitDomainName(codeBitName);
            var dirUrl = MetadataLoader.GetDirectoryUrl(domainName);
            if (dirUrl is null)
            {
                Console.Error.WriteLine($"No DNS directory entry for domain '{domainName}'.");
                Console.Error.WriteLine();
                return null;
            }
            var reader = MetadataLoader.GetDirectoryFromUrl(dirUrl);
            if (reader == null)
            {
                Console.Error.WriteLine($"Unable to read directory for domain '{domainName} from URL '{dirUrl}'.");
                Console.Error.WriteLine();
                return null;
            }

            var dirMetadata = reader.Find(codeBitName, version);

            if (dirMetadata is null)
            {
                Console.Error.WriteLine($"Codebit '{codeBitName}' v{version} is not listed in the directory.");
                Console.Error.WriteLine();
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
