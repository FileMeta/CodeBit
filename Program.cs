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
  Validate <filename>
    Validate the codebit metadata in the designated file.

  ValidateByUrl <url>
    Validate a codebit at the specified URL.

  ValidateByName <codeBitName> [-v <version>]
    Look up in a directory by name and validate.

  ValidateDirectory <domainName>
    Validate the published directory for a particular domain name.

  GetVersion
    Report the version of this codebit tool.
";

        static Action? s_operation;
        static string s_target = string.Empty;
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
            bool getTarget = false;

            var cl = new CommandLineLexer();
            // Get the command
            switch (cl.ReadNextArg()?.ToLowerInvariant())
            {
                case "validate":
                    s_operation = Validate;
                    getTarget = true;
                    break;

                case "validatebyurl":
                    s_operation = ValidateByUrl;
                    getTarget = true;
                    break;

                case "validatebyname":
                    s_operation = ValidateByName;
                    getTarget = true;
                    break;

                case "validatedirectory":
                    s_operation = ValidateDirectory;
                    getTarget = true;
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

            if (getTarget)
            {
                var target = cl.ReadNextArg();
                if (target == null)
                {
                    cl.ThrowValueError("Expected target for operation.");
                }
                s_target = target;
            }

            while (cl.MoveNext())
            {
                cl.ThrowIfNotOption();
                switch (cl.Current.ToLowerInvariant())
                {
                    case "-v":
                    case "-version":
                        s_version = SemVer.ParseForSearch(cl.ReadNextValue());
                        break;

                    default:
                        cl.ThrowUnexpectedArgError();
                        break;
                }
            }
        }

        static void Validate()
        {
            CodeBitValidator.ValidateFile(s_target);
        }

        static void ValidateByUrl()
        {
            CodeBitValidator.ValidatePublishedCodebit(s_target);
        }

        static void ValidateByName()
        {
            CodeBitValidator.ValidateByName(s_target, s_version);
        }

        static void ValidateDirectory()
        {
            CodeBitValidator.ValidateDirectory(s_target);
        }

        static void GetSyntax()
        {
            Console.WriteLine(c_syntax);
        }

        static void GetVersion()
        {
            Console.WriteLine(new FileMeta.AssemblyMetadata(typeof(Program)).Summary);
        }
    }
}
