using System;
using System.IO;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class AppFileTest
    {
        [TestMethod]
        public void ReadWriteFile()
        {
            const string folder = @"D:\Users\Murad\Documents\ImgSoh\Test\AppFile";
            const string name = "gab_org.jpg";
            const string subfolder = "sub";
            var subpath = Path.Combine(folder, subfolder);
            if (Directory.Exists(subpath)) {
                Directory.Delete(subpath, true);
            }

            var filename = Path.Combine(folder, name);
            var data = AppFile.ReadFile(filename);
            var subfilename = Path.Combine(subpath, name);
            AppFile.WriteFile(subfilename, data);
        }

        [TestMethod]
        public void ReadWriteEncryptedFile()
        {
            const string folder = @"D:\Users\Murad\Documents\ImgSoh\Test\AppFile";
            const string name = "gab_org.jpg";
            const string subfolder = "sub";
            var subpath = Path.Combine(folder, subfolder);
            if (Directory.Exists(subpath)) {
                Directory.Delete(subpath, true);
            }

            var filename = Path.Combine(folder, name);
            var data = AppFile.ReadFile(filename);
            const string subname = "0f3912";
            var subfilename = Path.Combine(subpath, subname);
            AppFile.WriteEncryptedFile(subfilename, data);

            var rdata = AppFile.ReadEncryptedFile(subfilename);
            Assert.AreEqual(data.Length, rdata.Length);
            Assert.AreEqual(data[100], rdata[100]);
        }

        [TestMethod]
        public void GetFilename()
        {
            const string subname = "0f3912";
            const string folder = @"D:\Users\Murad\Documents\ImgSoh\Test\AppFile";
            const string subfolder = "sub";
            var subpath = Path.Combine(folder, subfolder);
            var filename = AppFile.GetFileName(subname, subpath);
            Assert.AreEqual(filename, @"D:\Users\Murad\Documents\ImgSoh\Test\AppFile\sub\0\f\0f3912");
        }

        [TestMethod]
        public void GetRecycledName()
        {
            const string folder = @"D:\Users\Murad\Documents\ImgSoh\Test\AppFile";
            const string name = "gab_org";
            const string ext = "jpeg";
            const string subfolder = "sub";
            var subpath = Path.Combine(folder, subfolder);
            var filename = AppFile.GetRecycledName(name, ext, subpath, new DateTime(2024, 6, 13, 10, 23, 59));
            Assert.AreEqual(filename, @"D:\Users\Murad\Documents\ImgSoh\Test\AppFile\sub\2024-06-13\102359.gab_org.jpeg");
        }
    }
}