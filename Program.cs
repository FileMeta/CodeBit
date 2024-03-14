﻿using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using Bredd;
using FileMeta;

/* Feature Backlog
* ToJson calculates the SHA-256 hash as part of its operation
* Validation checks hashes
* Get, with hash check
*/

namespace CodeBit
{
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


        static Action? s_operation;
        static string s_target = string.Empty;
        static TargetType s_targetType = TargetType.Unknown;
        static SemVer? s_version = null;

        static void Main(string[] args)
        {
            try
            {
                ParseCommandLine();
                Debug.Assert(s_operation != null);
                s_operation();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }

        static void ParseCommandLine()
        {
            TargetType defaultTargetType = TargetType.Unknown;

            var cl = new CommandLineLexer();
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

        static void Get() {
            if (s_targetType == TargetType.Unknown || string.IsNullOrWhiteSpace(s_target)) {
                Console.Error.WriteLine("No source specified for Get command.");
                return;
            }

            // If targetType is by name, retrieve the metadata from the directory
            CodeBitMetadata? dirMetadata = null;
            string url;
            if (s_targetType == TargetType.CodebitName) {
                // Throws ApplicationException if not found.
                dirMetadata = MetadataLoader.ReadCodeBitFromName(s_target, s_version);
                url = dirMetadata.Url;
            }
            else if (s_targetType == TargetType.CodebitUrl) {
                url = s_target;
            }
            else {
                Console.Error.WriteLine("Get command requires either codebit name or url.");
                return;
            }

            // Get the codebit stream
            using var stream = Http.Get(url, "CodeBit");
            Debug.Assert(stream.CanSeek);

            // Read the metadata from the codebit
            var metadata = MetadataLoader.ReadCodeBitFromStream(stream);

            // Make sure the metadata matches
            // Look for an existing file and get permission to overwrite
            // Copy the stream to the file.

        }

        static void Validate()
        {
            switch (s_targetType)
            {
                case TargetType.Filename:
                    CodeBitValidator.ValidateFile(s_target);
                    break;

                case TargetType.CodebitUrl:
                    CodeBitValidator.ValidatePublishedCodebit(s_target);
                    break;

                case TargetType.CodebitName:
                    CodeBitValidator.ValidateByName(s_target, s_version);
                    break;

                case TargetType.DirectoryDomain:
                    CodeBitValidator.ValidateDirectory(s_target);
                    break;

                default:
                    Console.Error.WriteLine("No target specified for Validate command.");
                    break;
            }
        }

        static void ToJson()
        {
            if (s_targetType == TargetType.Unknown || string.IsNullOrWhiteSpace(s_target))
            {
                Console.Error.WriteLine("No target specified for ToJson command.");
                return;
            }
            CodeBitMetadata? metadata = MetadataLoader.Read(s_target, s_targetType, s_version);
            if (metadata == null)
            {
                Console.Error.WriteLine($"Target '{s_target}' not found.");
                return;
            }

            metadata.ToJson(Console.Out);
        }

        static void GetSyntax()
        {
            Console.WriteLine(c_syntax);
        }

        static void GetVersion()
        {
            Console.WriteLine(new FileMeta.AssemblyMetadata(typeof(Program)).Summary);
        }

        static void Test() {
            using (var instr = new LineEndFilterStream(File.OpenRead(@"C:\Users\brand\Source\FileMeta\CodeBit\CodeBits\CommandLineLexer.cs"))) {
                using (var outstr = File.Create(@"C:\Users\brand\downloads\CommandLineLexer.cs")) {
                    instr.CopyTo(outstr);
                }
            }
        }
    } // Class Program

    enum TargetType
    {
        Unknown,
        Filename,
        CodebitUrl,
        CodebitName,
        DirectoryDomain
    }

}
