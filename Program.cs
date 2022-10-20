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
                Console.WriteLine("Press any key to exit.");
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
            try
            {
                var path = Path.GetFullPath(s_target);
                Console.WriteLine($"Validating CodeBit metadata in '{path}'...");
                using (var reader = new StreamReader(path, Encoding.UTF8, true))
                {
                    (CodeBitMetadata metadata, ValidationLevel validationLevel, string validationDetail)
                        = CodeBitMetadata.ReadAndValidate(reader);

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

                    Console.WriteLine("name: " + metadata.Name);
                    Console.WriteLine("version: " + metadata.Version);
                    Console.WriteLine("url: " + metadata.Url);
                    Console.WriteLine("datePublished: " + metadata.DatePublished.ToString("O"));
                    Console.WriteLine("author: " + metadata.Author);
                    Console.WriteLine("description: " + metadata.Description);
                    Console.WriteLine("license: " + metadata.License);
                    Console.WriteLine("keywords: " + String.Join("; ", metadata.Keywords));
                    foreach(var pair in metadata.OtherProperties)
                    {
                        Console.WriteLine(String.Concat(pair.Key + ": " + pair.Value));
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine($"Failed to read and parse CodeBit: {err.Message}");
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
