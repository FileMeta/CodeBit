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
  CodeBit GetVersion
";

        static Action s_operation;
        static string s_target;

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

#if DEBUG
            if (Debugger.IsAttached)
            {
                Console.Write("\nPress any key to exit.");
                Console.ReadKey();
            }
#endif
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
            CodeBitValidator.ValidateFile(s_target);
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
