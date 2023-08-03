using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using Bredd;
using FileMeta;

namespace CodeBit
{
    internal class Program
    {
        const string c_syntax =
@"Syntax:
  CodeBit <Action> [arguments...]

Actions:
  Validate [-file] <filename>
    Validate the codebit metadata in the designated file.
    '-file' argument prefix is optional as it defaults to expecting a filename.

  Validate -url <url>
    Validate a codebit at the specified URL.

  Validate -name <codeBitName> [-v <version>]
    Look up in a directory by name and validate.

  Validate -dir <domainName>
    Validate the published directory for a particular domain name.

  ToJson [-file] <filename>
    Convert the codebit metadata to JSON suitable for adding to a directory.
    '-file' argument prefix is optional as it defaults to expecting a filename.

  GetVersion
    Report the version of this codebit tool.
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
            if (s_targetType == TargetType.DirectoryDomain)
            {
                Console.Error.WriteLine("Directory to JSON not (yet) supported.");
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
