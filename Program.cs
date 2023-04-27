using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using Bredd;

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

  ValidateDirectory <domainName>
    Validate the published directory for a particular domain name.

  GetVersion
    Report the version of this codebit tool.
";

        static Action? s_operation;
        static string? s_target;

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
                s_target = cl.ReadNextArg();
                if (s_target == null)
                {
                    cl.ThrowValueError("Expected target for operation.");
                }
            }

            while (cl.MoveNext())
            {
                cl.ThrowIfNotOption();
                switch (cl.Current.ToLowerInvariant())
                {
                    default:
                        cl.ThrowUnexpectedArgError();
                        break;
                }
            }
        }

        static void Validate()
        {
            CodeBitValidator.ValidateFile(s_target ?? string.Empty);
        }

        static void ValidateDirectory()
        {
            CodeBitValidator.ValidateDirectory(s_target ?? string.Empty);
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
