using System.Diagnostics;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;

namespace Test
{
    [TestClass]
    public class ExifHelperTests
    {
        [TestMethod]
        public void Single()
        {
            var name = $"DataSet1\\gab_org.jpg";
            var imagedata = File.ReadAllBytes(name);
            var fingerprint = ExifHelper.GetFingerPrint(imagedata);
            Debug.WriteLine(fingerprint);
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

            var vectors = new Tuple<string, string[]>[images.Length];
            for (var i = 0; i < images.Length; i++) {
                var name = $"DataSet1\\{images[i]}.jpg";
                var imagedata = File.ReadAllBytes(name);
                var fingerprint = ExifHelper.GetFingerPrint(imagedata);
                vectors[i] = new Tuple<string, string[]>(name, fingerprint);
            }

            for (var i = 0; i < vectors.Length; i++) {
                var match = ExifHelper.GetMatch(vectors[0].Item2, vectors[i].Item2);
                Debug.WriteLine($"{images[i]} = {match}");
            }
        }
    }
}
