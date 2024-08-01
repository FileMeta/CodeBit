using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using Bredd;
using FileMeta;

namespace CodeBit
{
    enum TargetType {
        Unknown,
        Filename,
        CodebitUrl,
        CodebitName,
        DirectoryDomain
    }

    internal class Program
    {
        const string c_syntax =
@"Syntax:
  CodeBit <Action> [arguments...]

Actions:
  Get [-name] <codeBitName> [-v <version>]
    Retrieve the codebit with the given name (and optional version) and
    place it in the current directory. The '-name' prefix is optional as
    retrieval by name is the default.

  Get -url <url>
    Retrieve the codebit at the specified URL and place it in the current
    directory.

  Validate [-file] <filename>
    Validate the codebit metadata in the designated file.
    The '-file' argument prefix is optional as it expects a filename.

  Validate -url <url>
    Validate a codebit at the specified URL.

  Validate -name <codeBitName> [-v <version>]
    Look up in a directory by name and validate.

  Validate -dir <domainName>
    Validate the a published directory.

  ToJson [-file] <filename>
    Convert the codebit metadata to JSON suitable for adding to a directory.
    The '-file' argument prefix is optional as it expects a filename.

  GetVersion
    Report the version of this codebit tool.

Arguments:
  -name <codeBitName>
    The name of a codebit. A codebit name is composed of a domain name
    followed by a path. For example, ""filemeta.org/sample.txt"".

  -url <url>
    The URL from which a codebit can be downloaded.

  -file <filename>
    The path and name of a file on the local computer. The path may be
    relative to the current directory.

  -dir <domainName>
    The domain name from which a directory can be referenced. The directory
    is identified using a specially-formatted DNS TXT record. See the CodeBit
    documentation for more information.

For the CodeBit specification and more information about CodeBits, see
https://FileMeta.org/CodeBit
";


        static Func<int>? s_operation;
        static string s_target = string.Empty;
        static TargetType s_targetType = TargetType.Unknown;
        static SemVer? s_version = null;

        static int Main(string[] args)
        {
            try
            {
                ParseCommandLine(Environment.CommandLine);
                Debug.Assert(s_operation is not null);
                return s_operation();
            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err.Message);
                return -2;
            }
        }

        internal static int TestMain(string args) {
            try {
                ParseCommandLine(args);
                if (s_operation is null)
                    throw new ApplicationException("ParseCommandLine failed to set an operation.");
                return s_operation();
            }
            catch (ApplicationException err) { // Only catch error-reporting exceptions. All others fall through and cause the test to fail.
                Console.Error.WriteLine(err.Message);
                return -2;
            }
        }

        static void ParseCommandLine(string args)
        {
            // Reset parsed values. This is mostly for unit tests that run multiple test with a single load. But it's good practice regardless.
            s_operation = null;
            s_target = string.Empty;
            s_targetType = TargetType.Unknown;
            s_version = null;

            TargetType defaultTargetType = TargetType.Unknown;

            var cl = new CommandLineLexer(args);
            // Get the command
            switch (cl.ReadNextArg()?.ToLowerInvariant())
            {
                case "get":
                    s_operation = Get;
                    defaultTargetType = TargetType.CodebitName;
                    break;

                case "validate":
                    s_operation = Validate;
                    defaultTargetType = TargetType.Filename;
                    break;

                case "tojson":
                    s_operation = ToJson;
                    defaultTargetType = TargetType.Filename;
                    break;

                case "getversion":
                    s_operation = GetVersion;
                    break;

                case "test":
                    s_operation = Test;
                    break;

                case null:
                case "help":
                case "-help":
                case "--help":
                case "man":
                case "-man":
                case "--man":
                case "manual":
                case "-manual":
                case "--manual":
                case "h":
                case "-h":
                case "-?":
                case "/h":
                case "/?":
                    s_operation = GetSyntax;
                    break;

                default:
                    Console.WriteLine($"Unknown command \"{cl.Current}\".");
                    Console.WriteLine($"For syntax: CodeBit -h");
                    s_operation = NullOperation;
                    return;
            }

            while (cl.MoveNext())
            {
                if (cl.IsOption)
                {
                    switch (cl.Current.ToLowerInvariant())
                    {
                        case "-file":
                            if (s_targetType != TargetType.Unknown) continue; // Take the first target found
                            s_target = cl.ReadNextValue();
                            s_targetType = TargetType.Filename;
                            break;

                        case "-url":
                            if (s_targetType != TargetType.Unknown) continue; // Take the first target found
                            s_target = cl.ReadNextValue();
                            s_targetType = TargetType.CodebitUrl;
                            break;

                        case "-name":
                            if (s_targetType != TargetType.Unknown) continue; // Take the first target found
                            s_target = cl.ReadNextValue();
                            s_targetType = TargetType.CodebitName;
                            break;

                        case "-dir":
                            if (s_targetType != TargetType.Unknown) continue; // Take the first target found
                            s_target = cl.ReadNextValue();
                            s_targetType = TargetType.DirectoryDomain;
                            break;

                        case "-v":
                        case "-version":
                            s_version = SemVer.ParseForSearch(cl.ReadNextValue());
                            break;

                        default:
                            cl.ThrowUnexpectedArgError();
                            break;
                    }
                }
                else
                {
                    if (s_targetType != TargetType.Unknown) continue; // Take the first target found
                    s_target = cl.Current;
                    s_targetType = defaultTargetType;
                }
            }
        }

        static int Get() {
            if (s_targetType == TargetType.Unknown || string.IsNullOrWhiteSpace(s_target)) {
                Console.Error.WriteLine("No source specified for Get command.");
                return -1;
            }

            ValidationLevel validationLevel;
            string validationDetail;

            // If targetType is by name, retrieve the metadata from the directory
            CodeBitMetadata? dirMetadata = null;
            string url;
            if (s_targetType == TargetType.CodebitName) {
                // Throws ApplicationException if not found.
                dirMetadata = MetadataLoader.ReadCodeBitFromName(s_target, s_version);
                (validationLevel, validationDetail) = dirMetadata.Validate();
                if (validationLevel >= ValidationLevel.FailMandatory)
                {
                    Console.Error.WriteLine($"Error: Directory metadata for '{s_target}' fails validation.");
                    Console.Error.WriteLineIndented(3, validationDetail);
                    return -1;
                }
                url = dirMetadata.Url;
            }
            else if (s_targetType == TargetType.CodebitUrl) {
                url = s_target;
            }
            else {
                Console.Error.WriteLine("Get command requires either codebit name or url.");
                return -1;
            }

            // Get the codebit stream
            using var stream = Http.Get(url, "CodeBit");
            Debug.Assert(stream.CanSeek);

            // Read the metadata from the codebit
            var metadata = MetadataLoader.ReadCodeBitFromStream(stream);
            (validationLevel, validationDetail) = metadata.Validate();
            if (validationLevel >= ValidationLevel.FailMandatory)
            {
                Console.Error.WriteLine($"Error: CodeBit fails validation");
                Console.Error.WriteLineIndented(3, validationDetail);
                return -1;
            }

            // If retrieved by name from the directory, test for match
            if (dirMetadata is not null)
            {
                (validationLevel, validationDetail) = metadata.CompareTo(dirMetadata, "CodeBit", "Directory", true);
                if (validationLevel > ValidationLevel.FailMandatory)
                {
                    Console.Error.WriteLine("Error: CodeBit metadata doesn't match directory.");
                    Console.Error.WriteLineIndented(3, validationDetail);
                    return -1;
                }
            }

            // Check for an existing file
            var filename = metadata.FilenameFromName;
            if (File.Exists(filename))
            {
                // Attempt to read the existing file's metadata
                var fileMetadata = MetadataLoader.ReadCodeBitFromFile(filename);
                if (string.Equals(fileMetadata.Hash, metadata.Hash))
                {
                    Console.Error.WriteLine($"Existing codebit '{filename}' matches online. No update performed.");
                    return 0;
                }

                if (!fileMetadata.Version.Equals(SemVer.Zero))
                {
                    var cmp = fileMetadata.Version.CompareTo(metadata.Version);
                    var cmpStr = (cmp < 0) ? "older than" : (cmp > 0) ? "newer than" : "same as";

                    Console.WriteLine($"Existing file '{filename}' version v{fileMetadata.Version} is {cmpStr} CodeBit version v{metadata.Version}.");
                    if (cmp == 0) Console.WriteLine("However, the contents don't match.");
                }
                else
                {
                    Console.WriteLine($"File '{filename}' exists.");
                }
                Console.WriteLine("Overwrite (y/n)?");
                var answer = Console.ReadLine()?.ToLower();

                if (answer != "y" && answer != "yes")
                    return -1;
            }

            // Finally, all is OK. Copy over the CodeBit!
            using (var outFile = File.Create(filename)) {
                stream.Position = 0;
                stream.CopyTo(outFile);
            }           
            Console.WriteLine($"Downloaded CodeBit '{filename}'.");
            return 0;
        }

        static int Validate()
        {
            switch (s_targetType)
            {
                case TargetType.Filename:
                    return CodeBitValidator.ValidateFile(s_target);

                case TargetType.CodebitUrl:
                    return CodeBitValidator.ValidatePublishedCodebit(s_target);

                case TargetType.CodebitName:
                    return CodeBitValidator.ValidateByName(s_target, s_version);

                case TargetType.DirectoryDomain:
                    return CodeBitValidator.ValidateDirectory(s_target);

                default:
                    Console.Error.WriteLine("No target specified for Validate command.");
                    return -1;
            }
        }

        static int ToJson()
        {
            if (s_targetType == TargetType.Unknown || string.IsNullOrWhiteSpace(s_target))
            {
                Console.Error.WriteLine("No target specified for ToJson command.");
                return -1;
            }
            CodeBitMetadata? metadata = MetadataLoader.Read(s_target, s_targetType, s_version);
            if (metadata == null)
            {
                Console.Error.WriteLine($"Target '{s_target}' not found.");
                return -1;
            }

            metadata.ToJson(Console.Out);
            return 0;
        }

        static int GetSyntax()
        {
            Console.WriteLine(c_syntax);
            return 0;
        }

        static int GetVersion()
        {
            Console.WriteLine(new FileMeta.AssemblyMetadata(typeof(Program)).Summary);
            return 0;
        }

        static int NullOperation()
        {
            return 0;
        }

        static int Test() {
            using (var instr = new LineEndFilterStream(File.OpenRead(@"C:\Users\brand\Source\FileMeta\CodeBit\CodeBits\CommandLineLexer.cs"))) {
                using (var outstr = File.Create(@"C:\Users\brand\downloads\CommandLineLexer.cs")) {
                    instr.CopyTo(outstr);
                }
            }
            return 0;
        }
    } // Class Program

    public static class TestHarness {
        public static int Run(string args) {
            return Program.TestMain(args);
        }
    }

}
