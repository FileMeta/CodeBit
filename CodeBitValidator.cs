using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
                    (metadata, validationLevel, validationDetail) = CodeBitMetadata.ReadAndValidate(reader);
                }

                if (validationLevel == ValidationLevel.Pass)
                {
                    Console.WriteLine("CodeBit metadata passes validation.");
                }
                else if (validationLevel == ValidationLevel.PassMandatory)
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
                if (validationLevel > ValidationLevel.PassMandatory) return;
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
                    (pubMetadata, pubValidationLevel, pubValidationDetail) = CodeBitMetadata.ReadAndValidate(reader);
                }

                if (pubValidationLevel == ValidationLevel.Pass)
                {
                    Console.WriteLine("Published CodeBit metadata passes validation.");
                }
                else if (pubValidationLevel == ValidationLevel.PassMandatory)
                {
                    Console.WriteLine("Warning: Published CodeBit fails one or more recommended but optional requirements:");
                    Console.WriteLine(pubValidationDetail);
                }
                else
                {
                    Console.WriteLine("Published CodeBit fails one or more mandatory requirements:");
                    Console.Write(pubValidationDetail);
                }

                if (validationLevel > ValidationLevel.PassMandatory) return;
            }
            catch (Exception err)
            {
                Console.WriteLine($"Failed to read and parse published CodeBit: {err.Message}");
                return;
            }

            int cmp = CompareSemVer(metadata.Version, pubMetadata.Version);
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

        static int CompareSemVer(string a, string b)
        {
            string[] aParts = (a != null) ? a.Split('.') : new string[0];
            string[] bParts = (b != null) ? b.Split('.') : new string[0];

            int min = Math.Min(aParts.Length, bParts.Length);
            for (int i = 0; i < min; ++i)
            {
                int cmp;
                if (int.TryParse(aParts[i], out int aInt) && int.TryParse(bParts[i], out int bInt))
                {
                    cmp = aInt - bInt;
                }
                else
                {
                    cmp = string.CompareOrdinal(aParts[i], bParts[i]);
                }
                if (cmp != 0) return cmp;
            }
            return aParts.Length - bParts.Length;
        }

#if TESTSEMVER

        static void TestCompareSemVer(string a, string b, int expected)
        {
            int result = CompareSemVer(a, b);
            bool match = (expected > 0 == result > 0) && (expected < 0 == result < 0);
            Console.WriteLine($"{a} {(result < 0 ? "<" : result > 0 ? ">" : "==")} {b} ({(match ? "correct" : "incorrect")})");
        }

        public static void UnitTestCompareSemVer()
        {
            TestCompareSemVer("1", "1.1", -1);
            TestCompareSemVer("2", "1.1", 1);
            TestCompareSemVer("1.2.3.4", "1.2.3.4", 0);
            TestCompareSemVer("1.2.2.10", "1.2.2.3", 1);
            TestCompareSemVer("1.2.3.four", "1.2.3.4", 1);
            TestCompareSemVer("1.3.2", "1.12.4", -1);
        }

#endif // TESTSEMVER

        /*
        static (bool match, string detail) CompareMetadata(CodeBitMetadata a, CodeBitMetadata b)
        {
            if (!string.Equals(a.Version, b.Version))
        }
        */
    }
}
 