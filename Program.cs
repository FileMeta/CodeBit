using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileMeta;
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
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
#endif
        }

        static void ParseCommandLine()
        {
            var cl = new CommandLineLexer();
            // Get the command
            switch (cl.ReadNextArg()?.ToLowerInvariant())
            {
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
                cl.ThrowIfNotOption();
                switch (cl.Current.ToLowerInvariant())
                {
                    default:
                        cl.ThrowUnexpectedArgError();
                        break;
                }
            }
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
