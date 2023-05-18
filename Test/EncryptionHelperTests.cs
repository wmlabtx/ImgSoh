using System.IO;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Test
{
    [TestClass()]
    public class EncryptionHelperTests
    {
        [TestMethod]
        public void TestEncryption()
        {
            var a1 = new byte[] { 0x10, 0x11, 0x12, 0x13 };
            var ea = EncryptionHelper.Encrypt(a1, "01234567");
            Assert.AreNotEqual(a1[0], ea[0]);
            var a2 = EncryptionHelper.Decrypt(ea, "01234567");
            Assert.IsTrue(a1.SequenceEqual(a2));
            var a3 = EncryptionHelper.Decrypt(ea, "01234568");
            Assert.IsNull(a3);

            var d1 = File.ReadAllBytes(@"Encryption\\0jlhuhkh.dat");
            var a4 = EncryptionHelper.DecryptDat(d1, "0jlhuhkh");
            Assert.IsTrue(a4 != null && a4.Length == 44881);

            var d2 = File.ReadAllBytes(@"Encryption\\ty004kjxn2i4.mzx");
            var a5 = EncryptionHelper.Decrypt(d2, "ty004kjxn2i4");
            Assert.IsTrue(a5 != null && a5.Length == 26269);
        }
    }
}
