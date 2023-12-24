using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ColorHelperTests
    {
        [TestMethod]
        public void Range()
        {
            double lmin = double.MaxValue, lmax = double.MinValue;
            double amin = double.MaxValue, amax = double.MinValue;
            double bmin = double.MaxValue, bmax = double.MinValue;
            for (var rb = 0; rb < 256; rb++) {
                for (var gb = 0; gb < 256; gb++) {
                    for (var bb = 0; bb < 256; bb++) {
                        ColorHelper.RgbToLab(rb, gb, bb, out var l, out var a, out var b);
                        lmin = Math.Min(lmin, l);
                        lmax = Math.Max(lmax, l);
                        amin = Math.Min(amin, a);
                        amax = Math.Max(amax, a);
                        bmin = Math.Min(bmin, b);
                        bmax = Math.Max(bmax, b);
                    }
                }
            }

            Debug.Print($"lmin = {lmin:F10}");
            Debug.Print($"lmax = {lmax:F10}");
            Debug.Print($"amin = {amin:F10}");
            Debug.Print($"amax = {amax:F10}");
            Debug.Print($"as = {amax - amin:F10}");
            Debug.Print($"bmin = {bmin:F10}");
            Debug.Print($"bmax = {bmax:F10}");
            Debug.Print($"bs = {bmax - bmin:F10}");

            /*
lmin = 0.0000000000
lmax = 0.9999999935
amin = -0.2338875742
amax = 0.2762167535
as = 0.5101043277
bmin = -0.3115281477
bmax = 0.1985697547
bs = 0.5100979023
            */
        }

        [TestMethod]
        public void QuantRange()
        {
            int lmin = int.MaxValue, lmax = int.MinValue;
            int amin = int.MaxValue, amax = int.MinValue;
            int bmin = int.MaxValue, bmax = int.MinValue;
            for (var rb = 0; rb < 256; rb++) {
                for (var gb = 0; gb < 256; gb++) {
                    for (var bb = 0; bb < 256; bb++) {
                        ColorHelper.RgbToQuantLab(rb, gb, bb, out int li, out int ai, out int bi);
                        lmin = Math.Min(lmin, li);
                        lmax = Math.Max(lmax, li);
                        amin = Math.Min(amin, ai);
                        amax = Math.Max(amax, ai);
                        bmin = Math.Min(bmin, bi);
                        bmax = Math.Max(bmax, bi);
                    }
                }
            }

            Debug.Print($"lmin = {lmin}");
            Debug.Print($"lmax = {lmax}");
            Debug.Print($"amin = {amin}");
            Debug.Print($"amax = {amax}");
            Debug.Print($"bmin = {bmin}");
            Debug.Print($"bmax = {bmax}");

            /*
    lmin = 0.0000000000
    lmax = 0.9999999935
    amin = -0.2338875742
    amax = 0.2762167535
    as = 0.5101043277
    bmin = -0.3115281477
    bmax = 0.1985697547
    bs = 0.5100979023
            */
        }

        [TestMethod]
        public void CreateTable()
        {
            var table = new byte[256 * 256 * 256 * 3];
            for (var rb = 0; rb < 256; rb++) {
                for (var gb = 0; gb < 256; gb++) {
                    for (var bb = 0; bb < 256; bb++) {
                        var offset = ((rb << 16) | (gb << 8) | bb) * 3;
                        ColorHelper.RgbToQuantLab(rb, gb, bb, out int li, out int ai, out int bi);
                        table[offset] = (byte)li;
                        table[offset + 1] = (byte)ai;
                        table[offset + 2] = (byte)bi;
                    }
                }
            }

            File.WriteAllBytes(AppConsts.FileRgbLab, table);
        }

        /*
        [TestMethod]
        public void RgbToLabFastTest()
        {
            ColorHelper.LoadTable(null);
            ColorHelper.RgbToQuantLab(0, 100, 00, out int li1, out int ai1, out int bi1);
            ColorHelper.RgbToLabFast(0, 100, 00, out int li2, out int ai2, out int bi2);
        }
        */

        /*
        [TestMethod]
        public void GetDistance()
        {
            ColorHelper.LoadTable(null);
            var images = new[] {
                "gab_org", "gab_bw", "gab_scale", "gab_flip", "gab_r90", "gab_crop", "gab_toside",
                "gab_blur", "gab_exp", "gab_logo", "gab_noice", "gab_r3", "gab_r10",
                "gab_face", "gab_sim1", "gab_sim2",
                "gab_nosim1", "gab_nosim2", "gab_nosim3", "gab_nosim4", "gab_nosim5", "gab_nosim6"
            };

            var vectors = new List<Tuple<string, byte[]>>();
            for (var i = 1; i <= 73; i++) {
                var name = $"DataSet2\\lexie-179-{i:D3}.jpg";
                var imagedata = File.ReadAllBytes(name);
                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    var vector = ColorHelper.CalculateVector(bitmap);
                    vectors.Add(new Tuple<string, byte[]>(name, vector));
                }
            }

            for (var i = 0; i < vectors.Count; i++) {
                var distance = ColorHelper.GetDistance(vectors[0].Item2, vectors[i].Item2);
                Debug.WriteLine($"{vectors[i].Item1} = {distance:F4}");
            }
        }
        */
    }
}