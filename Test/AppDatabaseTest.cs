using System;
using System.Drawing;
using System.IO;
using System.Linq;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class AppDatabaseTest
    {
        [TestMethod]
        public void LoadImportDelete()
        {
            /*
            AppVit.LoadNet(null);
            const string filedatabase = @"D:\Users\Murad\Documents\ImgSoh\Test\AppDatabase\spacer.db";
            AppDatabase.LoadImages(filedatabase, null);
            var orgfilename = @"D:\Users\Murad\Documents\ImgSoh\Test\AppDatabase\gab_org.jpg";
            var orgname = Path.GetFileNameWithoutExtension(orgfilename).ToLowerInvariant();
            Assert.AreEqual(orgname, "gab_org");
            var orgext = Path.GetExtension(orgfilename);
            if (orgext.StartsWith(".")) {
                orgext = orgext.Substring(1);
            }

            Assert.AreEqual(orgext, "jpg");
            var imagedata = AppFile.ReadFile(orgfilename);
            var hash = AppHash.GetHash(imagedata);
            float[] vector;
            float magnitude;
            DateTime taken;
            int meta;
            using (var magickImage = AppBitmap.ImageDataToMagickImage(imagedata)) {
                AppBitmap.Read(orgfilename, out taken, out meta);
                using (var bitmap = AppBitmap.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    vector = AppVit.CalculateVector(bitmap).ToArray();
                    magnitude = AppVit.GetMagnitude(vector);
                }
            }

            var name = AppImgs.GetName(hash);
            var newfilename = AppFile.GetFileName(name, AppConsts.PathHp);

            var lastview = DateTime.Now;
            var imgnew = new Img(
                hash: hash,
                name: name,
                vector: vector,
                orientation: RotateFlipType.RotateNoneFlipNone,
                lastview: lastview,
                next: string.Empty,
                lastcheck: DateTime.Now,
                verified: false,
                horizon: string.Empty,
                counter: 0,
                taken: taken,
                meta: meta,
                family: string.Empty,
                magnitude: magnitude,
                rank: 0,
                viewed: 0
            );

            AppDatabase.AddImg(imgnew);
            AppImgs.Add(imgnew);

            var family = AppImgs.GetNewFamily();
            //Assert.AreEqual(family, 1);

            AppDatabase.ImgDelete(hash);
            AppImgs.Remove(hash);

            AppBitmap.StopExif();
            */
        }
    }
}