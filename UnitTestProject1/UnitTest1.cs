using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void NoCrash()
        {
            ConsoleApp1.Program.Main();
        }
    }
}
