using System.Diagnostics;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using File = System.IO.File;

namespace Test
{
    [TestClass]
    public class ExifHelperTests
    {
        /*
        [TestMethod]
        public void Single()
        {
            var file1 = $"DataSet1\\gab_org.jpg";
            var imagedata = File.ReadAllBytes(file1);
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage != null) {
                    var ext = magickImage.Format.ToString().ToLower();
                    var tempfilename = $"{AppConsts.PathGbProtected}\\temp.{ext}";
                    File.WriteAllBytes(tempfilename, imagedata);
                    var exifinfo = new ExifInfo(tempfilename);
                    File.Delete(tempfilename);
                    Debug.WriteLine($"{exifinfo.Taken}; {exifinfo.Items.Length} entries found");
                }
            }
        }

        [TestMethod]
        public void GetMatchSingle()
        {
            VitHelper.LoadNet(null);
            var images = new[] {
                "gab_org", "gab_bw", "gab_scale", "gab_flip", "gab_r90", "gab_crop", "gab_toside",
                "gab_blur", "gab_exp", "gab_logo", "gab_noice", "gab_r3", "gab_r10",
                "gab_face", "gab_sim1", "gab_sim2",
                "gab_nosim1", "gab_nosim2", "gab_nosim3", "gab_nosim4", "gab_nosim5", "gab_nosim6"
            };

            var vectors = new Tuple<string, ExifInfo>[images.Length];
            for (var i = 0; i < images.Length; i++) {
                var file = $"DataSet1\\{images[i]}.jpg";
                var imagedata = File.ReadAllBytes(file);
                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                    if (magickImage != null) {
                        var ext = magickImage.Format.ToString().ToLower();
                        var tempfilename = $"{AppConsts.PathGbProtected}\\temp.{ext}";
                        File.WriteAllBytes(tempfilename, imagedata);
                        var exifinfo = new ExifInfo(tempfilename);
                        vectors[i] = Tuple.Create(images[i], exifinfo);
                        File.Delete(tempfilename);
                        Debug.WriteLine($"{exifinfo.Taken}; {exifinfo.Items.Length} entries found");
                    }
                }
            }

            for (var i = 0; i < vectors.Length; i++) {
                var diff = vectors[i].Item2.GetMatch(vectors[1].Item2);
                Debug.WriteLine($"{vectors[i].Item1}='{diff}' LVS='{vectors[i].Item2.LastModifiedString}' LV='{vectors[i].Item2.Taken}'");
            }
        }
        */ 
    }
}
