using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeBitUnitTest {
    [TestClass]
    public class CodeBitTests {
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

        [TestMethod]
        public void T01_GetVersion() {
            TestAndValidate("GetVersion", "^CodeBit$", @"^Version \d+\.\d+\.\d+\.\d+");
        }

        [TestMethod]
        public void T02_Validate_VideoFeedback() {
            TestAndValidate("Validate VideoFeedback.html",
                "Local CodeBit metadata passes validation.",
                "Published CodeBit metadata passes validation.",
                "File and Published CodeBits match.",
                "Directory Entry metadata passes validation.",
                "File and Directory CodeBits match.");
        }

        [TestMethod]
        public void T03_Validate_Warning() {
            TestAndValidate("Validate WarningCodebit.html",
                "^Warning: Local CodeBit fails one or more recommended but optional requirements:$",
                "Property 'datePublished' is an invalid format.",
                "Multiple instances of property 'description'.");
        }

        [TestMethod]
        public void T04_Validate_Error() {
            TestAndValidate("Validate ErrorCodebit.html",
                "^Error: Local CodeBit fails one or more mandatory requirements:$",
                "Property 'name' is required but not present.",
                "Property 'url' is required but not present.");
        }

        [TestMethod]
        public void T05_Validate_NotFound() {
            TestAndValidate("Validate NotFound.html",
                @"^CodeBit not found: NotFound\.html$");
        }

        [TestMethod]
        public void T06_Validate_URL() {
            TestAndValidate("Validate -url https://raw.githubusercontent.com/FileMeta/CodeBit/main/TestResources/VideoFeedback.html",
                "Published CodeBit metadata passes validation.",
                "Directory Entry metadata passes validation.",
                "Published Metadata and Directory CodeBits match.");
        }

        [TestMethod]
        public void T07_Validate_URL_404() {
            TestAndValidate("Validate -url https://raw.githubusercontent.com/FileMeta/CodeBit/main/TestResources/NotPresent404.html",
                "^Failed to read CodeBit from",
                "^404: Not Found$");
        }

        [TestMethod]
        public void T08_Validate_URL_BadDomain() {
            TestAndValidate("Validate -url https://invalid.example.com/BadDomain.html",
                "^Failed to read CodeBit from",
                "[Hh]ost"); // The OS reports this error so a different platform might need a different variation on this.
        }

        [TestMethod]
        public void T09_Validate_By_Name() {
            TestAndValidate("Validate -name sample.codebit.net/VideoFeedback.html",
                "Published CodeBit metadata passes validation.",
                "Published and Directory CodeBits match.");
        }

        [TestMethod]
        public void T10_Validate_By_Name_Version() {
            TestAndValidate("Validate -name sample.codebit.net/VideoFeedback.html -v 1.0.0-alpha",
                "Published CodeBit metadata passes validation.",
                "Published and Directory CodeBits fail one or more recommended but optional comparisons:",
                "Comment doesn't match");
        }

        [TestMethod]
        public void T11_Validate_SampleDirectory() {
            TestAndValidate("Validate -dir sample.codebit.net",
                "^DNS Success",
                "^Directory global metadata passes validation.$",
                // Only one entry has to pass for the following tests to match
                "^Directory Entry metadata passes validation.$",
                "^Published CodeBit metadata passes validation.$",
                "^Directory and Published CodeBits match.$",
                // Only one entry has to have a warning for the following test to match
                "Directory and Published CodeBits fail one or more recommended but optional comparisons:",
                // Allow the test to pass even as more entries are added
                @"^\d+ CodeBits in the directory.",
                @"^\d+ CodeBits with comparison warnings.",
                @"^\d+ Non-CodeBit source code entries in the directory.",
                @"^\d+ Non-Source Code entries in the directory.");
        }

        [TestMethod]
        public void T12_Validate_BadDirectory() {
            TestAndValidate("Validate -dir bad.codebit.net",
                @"^\d+ CodeBits in the directory.",
                @"^\d+ CodeBits failed validation.",
                @"^\d+ CodeBits with validation warnings.",
                @"^\d+ CodeBits failed comparison.",
                @"^\d+ CodeBits with comparison warnings.",
                @"^\d+ CodeBits in the directory.",
                @"^\d+ Non-CodeBit source code entries in the directory.",
                @"^\d+ Non-Source Code entries in the directory.");
        }

        [TestMethod]
        public void T13_ToJson_VideoFeedback() {
            TestAndValidate("ToJson VideoFeedback.html",
                @"^{",
                @"""@type"": ""SoftwareSourceCode""",
                @"""keywords"": ""CodeBit""",
                @"}$");
        }

        [TestMethod]
        public void T14_ToJson_By_Url() {
            TestAndValidate("ToJson -url https://raw.githubusercontent.com/FileMeta/CodeBit/509444cc55dcfba1e005ae1c439bed111c242193/TestResources/VideoFeedback.html",
                @"^{",
                @"""@type"": ""SoftwareSourceCode""",
                @"""version"": ""1.0.0-alpha""",
                @"""keywords"": ""CodeBit""",
                @"}$");
        }

        [TestMethod]
        public void T15_ToJson_By_Name() {
            TestAndValidate("ToJson -name sample.codebit.net/VideoFeedback.html",
                @"^{",
                @"""@type"": ""SoftwareSourceCode""",
                @"""version"": ""1.0.0""",
                @"""keywords"": ""CodeBit""",
                @"}$");
        }

        [TestMethod]
        public void T16_ToJson_By_NameVersion() {
            TestAndValidate("ToJson -name sample.codebit.net/VideoFeedback.html -v 1.0.0-alpha",
                @"^{",
                @"""@type"": ""SoftwareSourceCode""",
                @"""version"": ""1.0.0-alpha""",
                @"""keywords"": ""CodeBit""",
                @"}$");
        }

        [TestMethod]
        public void T17_Directory_404() {
            TestAndValidate("Validate -dir err404.codebit.net",
                @"Failed to read Directory",
                @"404: Not Found");
        }

        [TestMethod]
        public void T18_Directory_BadDomain() {
            TestAndValidate("Validate -dir baddomain.codebit.net",
                @"Failed to read Directory",
                @"[Hh]ost");
        }

        [TestMethod]
        public void T18_Directory_NoRecord() {
            TestAndValidate("Validate -dir phred.codebit.net",
                @"^No dir TXT record found on domain");
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
        CaptureWriter? m_stdOut;
        CaptureWriter? m_stdErr;

        public ConsoleCapture(StringBuilder capture) {
            m_capture = capture;
            m_stdOut = new CaptureWriter(this, Console.Out);
            m_stdErr = new CaptureWriter(this, Console.Error);
            Console.SetOut(m_stdOut);
            Console.SetError(m_stdErr);
        }

        public void Dispose() {
            if (m_stdErr is not null) {
                Console.SetError(m_stdErr.ChainOutput);
                m_stdErr = null;
            }
            if (m_stdOut is not null) {
                Console.SetOut(m_stdOut.ChainOutput);
                m_stdOut = null;
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
