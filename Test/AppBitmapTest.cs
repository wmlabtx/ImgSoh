using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageMagick;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class AppBitmapTest
    {
        [TestMethod]
        public void ReadPictures()
        {
            const string folder = @"D:\Users\Murad\Documents\ImgSoh\Test\AppBitmap";
            const string name_corrupted = "gab_corrupted.jpg";
            var m_corrupted = AppBitmap.ImageDataToMagickImage(AppFile.ReadFile(Path.Combine(folder, name_corrupted)));
            Assert.AreEqual(m_corrupted, null);
            const string name_org = "gab_org.jpg";
            var filename_org = Path.Combine(folder, name_org);
            var m_org = AppBitmap.ImageDataToMagickImage(AppFile.ReadFile(filename_org));
            Assert.AreNotEqual(m_org, null);
            Assert.AreEqual(m_org.Width, 2000);

            AppBitmap.Read(filename_org, out var taken, out var meta);
            Assert.AreEqual(taken, new DateTime(2013, 9, 8, 22, 46, 29));
            Assert.AreEqual(meta, 162);
            
            var b_r90 = AppBitmap.MagickImageToBitmap(m_org, RotateFlipType.Rotate90FlipNone);
            b_r90.Save(@"D:\Users\Murad\Documents\ImgSoh\Test\AppBitmap\gab_r90.jpg", ImageFormat.Jpeg);
            var b_cut = AppBitmap.ScaleAndCut(b_r90, 224, 16);
            b_cut.Save(@"D:\Users\Murad\Documents\ImgSoh\Test\AppBitmap\gab_cut.jpg", ImageFormat.Jpeg);

            const string name_logo = "gab_logo.jpg";
            var filename_logo = Path.Combine(folder, name_logo);
            var m_logo = AppBitmap.ImageDataToMagickImage(AppFile.ReadFile(filename_logo));
            AppBitmap.Composite(m_org, m_logo, out var m_logo_diff);
            m_logo_diff.Write(new FileInfo(@"D:\Users\Murad\Documents\ImgSoh\Test\AppBitmap\gab_logo_diff.jpg"), MagickFormat.Jpeg);

            const string name_compressed = "gab_compressed.jpg";
            var filename_compressed = Path.Combine(folder, name_compressed);
            var m_compressed = AppBitmap.ImageDataToMagickImage(AppFile.ReadFile(filename_compressed));
            AppBitmap.Composite(m_org, m_compressed, out var m_compressed_diff);
            m_compressed_diff.Write(new FileInfo(@"D:\Users\Murad\Documents\ImgSoh\Test\AppBitmap\gab_compressed_diff.jpg"), MagickFormat.Jpeg);

            const string name_crop = "gab_crop.jpg";
            var filename_crop = Path.Combine(folder, name_crop);
            var m_crop = AppBitmap.ImageDataToMagickImage(AppFile.ReadFile(filename_crop));
            AppBitmap.Composite(m_org, m_crop, out var m_crop_diff);
            m_crop_diff.Write(new FileInfo(@"D:\Users\Murad\Documents\ImgSoh\Test\AppBitmap\gab_crop_diff.jpg"), MagickFormat.Jpeg);

            AppBitmap.StopExif();
        }
    }
}