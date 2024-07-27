namespace CodeBitUnitTest {
    [TestClass]
    public class CodeBitUnitTest {


        [TestMethod]
        public void GetVersion() {
            CodeBit.TestHarness.Run("GetVersion");
        }

        [TestMethod]
        public void Booyah() {
            Console.WriteLine("Booyah!");
        }
    }
}
