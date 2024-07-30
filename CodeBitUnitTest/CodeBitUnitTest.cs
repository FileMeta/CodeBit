using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeBitUnitTest {
    [TestClass]
    public class CodeBitUnitTest {


        [TestMethod]
        public void GetVersion() {
            TestAndValidate("GetVersion", "CodeBit", @"Version \d+\.\d+\.\d+\.\d+");
        }

        [TestMethod]
        public void Booyah() {
            var sb = new StringBuilder();
            using (new ConsoleCapture(sb)) {
                Console.WriteLine("Booyah!");
            }
            Debug.Write("-");
            Debug.Write(sb.ToString());
            Debug.WriteLine("-");
        }

        void TestAndValidate(string command, params string[] rxTests) {
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
                Console.Write($"{rx}: ");
                var match = Regex.Match(output, rx);
                if (match.Success) {
                    WriteInColor("success", ConsoleColor.Green);
                }
                else {
                    WriteInColor("fail", ConsoleColor.Red);
                    success = false;
                }
                Console.WriteLine();
            }
            Assert.IsTrue(success);
        }

        static void WriteInColor(string text, ConsoleColor color) {
            var save = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = save;
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
                m_owner.m_capture.Append(value);
                m_chainOutput.Write(value);
            }

            public override Encoding Encoding => Encoding.Default;

            public TextWriter ChainOutput => m_chainOutput;

        }
    }
}
