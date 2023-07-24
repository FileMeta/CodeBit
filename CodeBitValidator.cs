//#define RAW
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using Bredd;

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
                CompareAndReport(fileMetadata, pubMetadata, "File", "Published");
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
            try
            {
                reader = MetadataLoader.GetDirectoryFromUrl(dirUrl);
            }
            catch (Exception err)
            {
                Console.WriteLine("Failed to read directory: " + err.Message);
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

                int nCodeBits = 0;
                int nSourceCode = 0;
                int nOther = 0;
                for (; ; )
                {
                    var codebitMetadata = reader.ReadCodeBit();
                    if (codebitMetadata == null) break;

                    if (codebitMetadata.IsCodeBit)
                    {
                        nCodeBits++;
                        (validationLevel, validationDetail) = codebitMetadata.Validate();

                        if (validationLevel == ValidationLevel.Pass)
                        {
                            Console.Out.WriteLine($"{codebitMetadata.Name} v{codebitMetadata.Version}: CodeBit directory entry passes validation.");
                        }
                        else if (validationLevel == ValidationLevel.FailRecommended)
                        {
                            Console.Out.WriteLine($"{codebitMetadata.Name} v{codebitMetadata.Version}: CodeBit directory entry fails one or more recommended but optional requirements:");
                            Console.Out.WriteLineIndented(3, validationDetail);
                        }
                        else
                        {
                            Console.Out.WriteLine($"{codebitMetadata.Name} v{codebitMetadata.Version}: CodeBit directory entry fails one or more mandatory requirements:");
                            Console.Out.WriteLineIndented(3, validationDetail);
                        }
                    }
                    else if (codebitMetadata.IsSoftwareSourceCode)
                    {
                        ++nSourceCode;
                    }
                    else
                    {
                        ++nOther;
                    }

                }

                Console.WriteLine($"{nCodeBits} CodeBits in the directory.");
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
            return ValidateAndReport(metadata);
        }

        private static (ValidationLevel validationLevel, CodeBitMetadata? metadata) ValidateAndReport(CodeBitMetadata metadata)
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
            return (validationLevel, metadata);
        }

        private static void CompareAndReport(CodeBitMetadata a, CodeBitMetadata b, string aLabel, string bLabel)
        {
            (ValidationLevel cmpValidationLevel, string cmpValidationDetail) = a.CompareTo(b, aLabel, bLabel);

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
        }

        private static void CompareWithDirectoryAndReport(CodeBitMetadata a, string aLabel)
        {
            Console.WriteLine($"Validating directory entry for '{a.Name}'...");

            string domainName = MetadataLoader.GetCodebitDomainName(a.Name);
            var dirUrl = MetadataLoader.GetDirectoryUrl(domainName);
            if (dirUrl is null)
            {
                Console.WriteLine($"No DNS directory entry for domain '{domainName}'.");
                Console.WriteLine();
                return;
            }
            var reader = MetadataLoader.GetDirectoryFromUrl(dirUrl);
            if (reader == null)
            {
                Console.WriteLine($"Unable to read directory for domain '{domainName} from URL '{dirUrl}'.");
                Console.WriteLine();
                return;
            }

            CodeBitMetadata? dirMetadata;
            for (; ; )
            {
                dirMetadata = reader.ReadCodeBit();
                if (dirMetadata is null)
                {
                    Console.WriteLine($"Codebit '{a.Name}' is not listed in the directory.");
                    Console.WriteLine();
                    return;
                }
                if (string.Equals(dirMetadata.Name, a.Name, StringComparison.Ordinal)) break;
            }

            (var validationLevel, var validationDetail) = ValidateAndReport(dirMetadata);
            if (validationLevel > ValidationLevel.FailRecommended) return;

            Console.WriteLine($"Comparing {aLabel} with directory...");
            CompareAndReport(a, dirMetadata, aLabel, "Directory");
        }

    }

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
 