//#define RAW
using System;
using System.Collections.Generic;
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
            var path = Path.GetFullPath(filename);
            Console.WriteLine($"Validating CodeBit metadata in '{path}'...");

            CodeBitMetadata metadata;
            ValidationLevel validationLevel;
            string validationDetail;

            try
            {
                metadata = MetadataLoader.ReadCodeBitFromFile(path);
                metadata.FilenameForValidation = filename;
                (validationLevel, validationDetail) = metadata.Validate();

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
                pubMetadata = MetadataLoader.ReadCodeBitFromUrl(metadata.Url);
                (pubValidationLevel, pubValidationDetail) = pubMetadata.Validate();

                if (pubValidationLevel == ValidationLevel.Pass)
                {
                    Console.WriteLine("Published CodeBit metadata passes validation.");
                }
                else if (pubValidationLevel == ValidationLevel.FailRecommended)
                {
                    Console.WriteLine("Warning: Published CodeBit fails one or more recommended but optional requirements:");
                    Console.Out.WriteLineIndented(3, pubValidationDetail);
                }
                else
                {
                    Console.WriteLine("Published CodeBit fails one or more mandatory requirements:");
                    Console.Out.WriteLineIndented(3, pubValidationDetail);
                }
                Console.WriteLine();

                if (validationLevel > ValidationLevel.FailRecommended) return;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Failed to read and parse published CodeBit: {err.Message}");
                return;
            }

            Console.WriteLine("Comparing local with published copy...");
            try
            {
                (ValidationLevel cmpValidationLevel, string cmpValidationDetail) = metadata.CompareTo(pubMetadata, "Local", "Published");

                if (cmpValidationLevel == ValidationLevel.Pass)
                {
                    Console.WriteLine("Local and Published CodeBits match.");
                }
                else if (cmpValidationLevel == ValidationLevel.FailRecommended)
                {
                    Console.WriteLine("Warning: Local and published codebits fail one or more recommended but optional comparisons:");
                    Console.Out.WriteLineIndented(3, cmpValidationDetail);
                }
                else
                {
                    Console.WriteLine("Error: Local and published codebits fail one or more required comparisons:");
                    Console.Out.WriteLineIndented(3, cmpValidationDetail);
                }
                Console.WriteLine();
            }
            catch (Exception err)
            {
                Console.WriteLine($"Failed to compare local and published CodeBit: {err.Message}");
                return;
            }

            Console.WriteLine("Comparing with directory...");
            try
            {
                string domainName = MetadataLoader.GetCodebitDomainName(metadata.Name);
                var dirUrl = MetadataLoader.GetDirectoryUrl(domainName);
                if (dirUrl is null)
                {
                    Console.WriteLine($"No DNS directory entry for domain '{domainName}'.");
                    return;
                }
                var reader = MetadataLoader.GetDirectoryFromUrl(dirUrl);

                CodeBitMetadata? dirMetadata;
                for (; ; )
                {
                    dirMetadata = reader.ReadCodeBit();
                    if (dirMetadata is null)
                    {
                        Console.WriteLine($"Codebit '{metadata.Name}' is not listed in the directory.");
                        return;
                    }
                    if (string.Equals(dirMetadata.Name, metadata.Name, StringComparison.Ordinal)) break;
                }

                (ValidationLevel cmpValidationLevel, string cmpValidationDetail) = metadata.CompareTo(dirMetadata, "Local", "Directory");

                if (cmpValidationLevel == ValidationLevel.Pass)
                {
                    Console.WriteLine("Local and Directory CodeBits match.");
                }
                else if (cmpValidationLevel == ValidationLevel.FailRecommended)
                {
                    Console.WriteLine("Warning: Local and directory codebits fail one or more recommended but optional comparisons:");
                    Console.Out.WriteLineIndented(3, cmpValidationDetail);
                }
                else
                {
                    Console.WriteLine("Error: Local and directory codebits fail one or more required comparisons:");
                    Console.Out.WriteLineIndented(3, cmpValidationDetail);
                }
                Console.WriteLine();
            }
            catch (Exception err)
            {
                Console.WriteLine($"Failed to retrieve directory or compare metadata: {err.Message}");
                return;
            }
        }

        public static void ValidatePublishedCodebit(string url)
        {

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

        /*
        static (bool match, string detail) CompareMetadata(CodeBitMetadata a, CodeBitMetadata b)
        {
            if (!string.Equals(a.Version, b.Version))
        }
        */
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
 