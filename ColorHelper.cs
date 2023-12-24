using OpenCvSharp.Dnn;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ImgSoh
{
    public static class ColorHelper
    {
        private static byte[] _rgblab;

        public static void LoadTable(IProgress<string> progress)
        {
            progress?.Report($"Loading table{AppConsts.CharEllipsis}");
            _rgblab = File.ReadAllBytes(AppConsts.FileRgbLab);
        }

        public static void RgbToLab(int rb, int gb, int bb, out double ld, out double ad, out double bd)
        {
            var r = ((double)rb) / byte.MaxValue;
            var g = ((double)gb) / byte.MaxValue;
            var b = ((double)bb) / byte.MaxValue;

            var l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
            var m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
            var s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

            var l3 = Math.Pow(l, 1.0 / 3.0);
            var m3 = Math.Pow(m, 1.0 / 3.0);
            var s3 = Math.Pow(s, 1.0 / 3.0);

            ld = 0.2104542553 * l3 + 0.7936177850 * m3 - 0.0040720468 * s3;
            ad = 1.9779984951 * l3 - 2.4285922050 * m3 + 0.4505937099 * s3;
            bd = 0.0259040371 * l3 + 0.7827717662 * m3 - 0.8086757660 * s3;
        }

        public static void RgbToQuantLab(int rb, int gb, int bb, out int li, out int ai, out int bi)
        {
            var r = ((double)rb) / byte.MaxValue;
            var g = ((double)gb) / byte.MaxValue;
            var b = ((double)bb) / byte.MaxValue;

            var l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
            var m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
            var s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;

            var l3 = Math.Pow(l, 1.0 / 3.0);
            var m3 = Math.Pow(m, 1.0 / 3.0);
            var s3 = Math.Pow(s, 1.0 / 3.0);

            var ld = 0.2104542553 * l3 + 0.7936177850 * m3 - 0.0040720468 * s3;
            var ad = 1.9779984951 * l3 - 2.4285922050 * m3 + 0.4505937099 * s3;
            var bd = 0.0259040371 * l3 + 0.7827717662 * m3 - 0.8086757660 * s3;

            li = (int)Math.Round(ld * 99 / 0.9999999935);
            ai = (int)Math.Round((ad + 0.2338875742) * 49 / 0.5101043277);
            bi = (int)Math.Round((bd + 0.3115281477) * 49 / 0.5100979023);
        }

        public static void RgbToLabFast(int rb, int gb, int bb, out int li, out int ai, out int bi)
        {
            var offset = ((rb << 16) | (gb << 8) | bb) * 3;
            li = _rgblab[offset];
            ai = _rgblab[offset + 1];
            bi = _rgblab[offset + 2];
        }

        public static byte[] CalculateVector(Bitmap bitmap)
        {
            var lvector = new long[200];
            using (var b = BitmapHelper.ScaleAndCut(bitmap, 448, 16)) {
                var bitmapdata = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                var stride = bitmapdata.Stride;
                var data = new byte[Math.Abs(bitmapdata.Stride * bitmapdata.Height)];
                Marshal.Copy(bitmapdata.Scan0, data, 0, data.Length);
                b.UnlockBits(bitmapdata);
                var offsety = 0;
                for (var y = 0; y < b.Height; y++) {
                    var offsetx = offsety;
                    for (var x = 0; x < b.Width; x++) {
                        var rbyte = data[offsetx + 2];
                        var gbyte = data[offsetx + 1];
                        var bbyte = data[offsetx];
                        offsetx += 3;

                        RgbToLabFast(rbyte, gbyte, bbyte, out var li, out var ai, out var bi);
                        lvector[li]++;
                        lvector[ai + 100]++;
                        lvector[bi + 150]++;
                    }

                    offsety += stride;
                }
            }

            var vector = new byte[200];
            long max = 0;
            for (var i = 0; i < 100; i++) {
                max = Math.Max(max, lvector[i]);
            }

            for (var i = 0; i < 100; i++) {
                vector[i] = (byte)Math.Round(lvector[i] * 255.0 / max);
            }

            max = 0;
            for (var i = 100; i < 150; i++) {
                max = Math.Max(max, lvector[i]);
            }

            for (var i = 100; i < 150; i++) {
                vector[i] = (byte)Math.Round(lvector[i] * 255.0 / max);
            }

            max = 0;
            for (var i = 150; i < 200; i++) {
                max = Math.Max(max, lvector[i]);
            }

            for (var i = 150; i < 200; i++) {
                vector[i] = (byte)Math.Round(lvector[i] * 255.0 / max);
            }

            return vector;
        }

        public static float GetDistance(byte[] x, byte[] y)
        {
            if (x.Length == 0 || y.Length == 0 || x.Length != y.Length) {
                return 1f;
            }

            var dot = 0.0;
            var magx = 0.0;
            var magy = 0.0;
            for (var n = 0; n < x.Length; n++) {
                dot += (double)x[n] * y[n] / (255.0 * 255.0);
                magx += (double)x[n] * x[n] / (255.0 * 255.0);
                magy += (double)y[n] * y[n] / (255.0 * 255.0);
            }

            return 1f - (float)(dot / (Math.Sqrt(magx) * Math.Sqrt(magy)));
        }
    }
}
