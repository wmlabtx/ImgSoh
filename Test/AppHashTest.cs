using System.IO;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class AppHashTest
    {
        [TestMethod]
        public void GetHash()
        {
            var imagedata = AppFile.ReadFile(@"D:\Users\Murad\Documents\ImgSoh\Test\AppHash\gab_org.jpg");
            var hash = AppHash.GetHash(imagedata);
            Assert.AreEqual(hash, "0f3912a3f0bb0c35768f3c8a043b9251");
        }
    }
}
