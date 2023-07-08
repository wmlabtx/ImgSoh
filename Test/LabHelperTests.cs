using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System;

namespace Test
{
    [TestClass]
    public class LabHelperTests
    {
        private static void Rgb(int rb, int gb, int bb)
        {
            LabHelper.RGBToLAB(rb, gb, bb, out var l, out var a, out var b);
            LabHelper.LABToRGB(l, a, b, out var rb2, out var gb2, out var bb2);
            Assert.AreEqual(rb, rb2);
            Assert.AreEqual(gb, gb2);
            Assert.AreEqual(bb, bb2);
        }

        [TestMethod]
        public void Convertions()
        {
            Rgb(0, 0, 0);
            Rgb(255, 0, 0);
            Rgb(0, 255, 0);
            Rgb(0, 0, 255);
            Rgb(255, 0, 255);
            Rgb(255, 255, 0);
            Rgb(0, 255, 255);
            Rgb(255, 255, 255);
        }

        [TestMethod]
        public void DetectBoundaries()
        {
            var lmin = double.MaxValue;
            var lmax = double.MinValue;
            var amin = double.MaxValue;
            var amax = double.MinValue;
            var bmin = double.MaxValue;
            var bmax = double.MinValue;
            for (var rb = 0; rb < 256; rb++) {
                for (var gb = 0; gb < 256; gb++) {
                    for (var bb = 0; bb < 256; bb++) {
                        LabHelper.RGBToLAB(rb, gb, bb, out var l1, out var a1, out var b1);
                        lmin = Math.Min(lmin, l1);
                        lmax = Math.Max(lmax, l1);
                        amin = Math.Min(amin, a1);
                        amax = Math.Max(amax, a1);
                        bmin = Math.Min(bmin, b1);
                        bmax = Math.Max(bmax, b1);
                    }
                }
            }

            Debug.WriteLine($"l = [{lmin:F4}, {lmax:F4}]");
            Debug.WriteLine($"a = [{amin:F4}, {amax:F4}]");
            Debug.WriteLine($"b = [{bmin:F4}, {bmax:F4}]");

            /*
                l = [0.0000, 0.1000]
                a = [-0.2339, 0.2762]
                b = [-0.3115, 0.1986]
            */
        }
    }
}
