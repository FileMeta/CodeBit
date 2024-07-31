using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeBitUnitTest {
    [TestClass]
    public class CodeBitUnitTest {
        const string c_testResourcesDir = "TestResources";

        [TestInitialize]
        public void TestInitialize() {
            // Change the current directory to TestResources
            var path = Environment.CurrentDirectory;
            if (string.Equals(Path.GetFileName(path), c_testResourcesDir))
                return; // Already in the right directory
            while (!Directory.Exists(Path.Combine(path, c_testResourcesDir))) {
                if (path is null || path.Length < 5)
                    Assert.Fail($"Resource directory '{c_testResourcesDir}' not found!");
                Console.WriteLine(path);
                path = Path.GetDirectoryName(path);
            }
            Environment.CurrentDirectory = Path.Combine(path, c_testResourcesDir);
        }

        [TestMethod()]
        public void T01_GetVersion() {
            TestAndValidate("GetVersion", "^CodeBit$", @"^Version \d+\.\d+\.\d+\.\d+");
        }

        [TestMethod()]
        public void T02_Validate_VideoFeedback() {
            TestAndValidate("Validate VideoFeedback.html");
        }


        void TestAndValidate(string command, params string[] rxTests) {
            Console.WriteLine();
            Console.WriteLine("Testing: " + command);

            Console.WriteLine("----------------");
            var capture = new StringBuilder();
            using (new ConsoleCapture(capture)) {
                CodeBit.TestHarness.Run(command);
            }
            Console.WriteLine("================");

            var output = capture.ToString();
            bool success = true;
            foreach(string rx in rxTests) {
                var match = Regex.Match(output, rx, RegexOptions.ExplicitCapture|RegexOptions.Multiline);
                Console.WriteLine($"{(match.Success ? "match:" : "miss: ")} {rx}");
                if (!match.Success)
                    success = false;
            }
            if (!success)
                Assert.Fail("Failed to match one or more expected outputs.");
        }
    }

    public class ConsoleCapture : IDisposable {

        StringBuilder m_capture;
        CaptureWriter? m_writer;

        public ConsoleCapture(StringBuilder capture) {
            m_capture = capture;
            m_writer = new CaptureWriter(this, Console.Out);
            Console.SetOut(m_writer);
        }

        public void Dispose() {
            if (m_writer != null) {
                Console.SetOut(m_writer.ChainOutput);
                m_writer = null;
            }
        }

        private class CaptureWriter : TextWriter {
            ConsoleCapture m_owner;
            TextWriter m_chainOutput;

            public CaptureWriter(ConsoleCapture owner, TextWriter outWriter) {
                m_owner = owner;
                m_chainOutput = outWriter;
            }

            public override void Write(char value) {
                if (value == '\r')
                    return; // Line endings are strictly '\n' for the sake of regex.
                m_owner.m_capture.Append(value);
                m_chainOutput.Write(value);
            }

            public override Encoding Encoding => Encoding.Default;

            public TextWriter ChainOutput => m_chainOutput;

        }
    }
}
