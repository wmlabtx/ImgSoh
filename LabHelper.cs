using System;

namespace ImgSoh
{
    public static class LabHelper
    {
        private static double Cbrt(double val)
        {
            return Math.Pow(val, 1.0 / 3.0);
        }

        private static double GammaToLinear(int val)
        {
            var abs = val / 255.0;
            return abs < 0.04045 ?
                abs / 12.92 :
                Math.Pow((abs + 0.055) / 1.055, 2.4);
        }

        private static int LinearToGamma(double val)
        {
            var g = val >= 0.0031308 ? 
                1.055 * Math.Pow(val, 1 / 2.4) - 0.055 : 
                val * 12.92;

            return (int)Math.Round(g * 255.0);
        }

        public static void RGBToLAB(int rb, int gb, int bb, out double l, out double a, out double b)
        {
            var rl = GammaToLinear(rb);
            var gl = GammaToLinear(gb);
            var bl = GammaToLinear(bb);
            var ld = 0.4122214708 * rl + 0.5363325363 * gl + 0.0514459929 * bl;
            var md = 0.2119034982 * rl + 0.6806995451 * gl + 0.1073969566 * bl;
            var sd = 0.0883024619 * rl + 0.2817188376 * gl + 0.6299787005 * bl;
            ld = Cbrt(ld); 
            md = Cbrt(md); 
            sd = Cbrt(sd);
            l = ld * +0.2104542553 + md * +0.7936177850 + sd * -0.0040720468;
            a = ld * +1.9779984951 + md * -2.4285922050 + sd * +0.4505937099;
            b = ld * +0.0259040371 + md * +0.7827717662 + sd * -0.8086757660;
            //l /= 5.0;
        }

        public static void LABToRGB(double l, double a, double b, out int rb, out int gb, out int bb)
        {
            //l *= 5.0;
            var ld = l + a * +0.3963377774 + b * +0.2158037573;
            var md = l + a * -0.1055613458 + b * -0.0638541728;
            var sd = l + a * -0.0894841775 + b * -1.2914855480;
            ld = ld * ld * ld; 
            md = md * md * md;
            sd = sd * sd * sd;
            var rd = ld * +4.0767416621 + md * -3.3077115913 + sd * +0.2309699292;
            var gd = ld * -1.2684380046 + md * +2.6097574011 + sd * -0.3413193965;
            var bd = ld * -0.0041960863 + md * -0.7034186147 + sd * +1.7076147010;
            rb = LinearToGamma(rd);
            gb = LinearToGamma(gd);
            bb = LinearToGamma(bd);
            rb = Math.Min(255, Math.Max(0, rb));
            gb = Math.Min(255, Math.Max(0, gb));
            bb = Math.Min(255, Math.Max(0, bb));
        }

        public static float GetDistance(ColorLAB x, ColorLAB y)
        {
            var ldiff = x.L - y.L;
            var adiff = x.A - y.A;
            var bdiff = x.B - y.B;

            var distance = (float)Math.Sqrt(ldiff * ldiff + adiff * adiff + bdiff * bdiff);
            return distance;
        }

        /*
        public static float[] CalculateVector(Bitmap bitmap)
        {
            var hist = new int[112, 112];
            var hmax = 0.0;
            using (var b = BitmapHelper.ScaleAndCut(bitmap, 480, 16)) 
            using (var mat = b.ToMat()) 
            using (var mat2 = new Mat())
            using (var mat3 = new Mat()) {
                mat.ToBitmap().Save("mat.png", ImageFormat.Png);
                Cv2.GaussianBlur(mat, mat2, new OpenCvSharp.Size(3, 3), 0);
                mat2.ToBitmap().Save("mat2.png", ImageFormat.Png);
                Cv2.Laplacian(mat2, mat3, MatType.CV_8UC3, 3);
                mat3.ToBitmap().Save("mat3.png", ImageFormat.Png);

                //b.Save("bitmap.png", ImageFormat.Png);
                var bitmapdata = bitmap.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                var stride = bitmapdata.Stride;
                var data = new byte[Math.Abs(bitmapdata.Stride * bitmapdata.Height)];
                Marshal.Copy(bitmapdata.Scan0, data, 0, data.Length);
                bitmap.UnlockBits(bitmapdata);
                var offsety = 0;
                for (var y = 0; y < b.Height; y++) {
                    var offsetx = offsety;
                    for (var x = 0; x < b.Width; x++) {
                        var rbyte = data[offsetx + 2];
                        var gbyte = data[offsetx + 1];
                        var bbyte = data[offsetx];
                        offsetx += 3;
                        BitmapHelper.RGB2LAB(rbyte, gbyte, bbyte, out var ld, out var ad, out var bd);
                        var xm = (int)Math.Round((ad + 0.4) * 112);
                        var ym = (int)Math.Round((bd + 0.4) * 112);
                        hist[xm, ym]++;
                        hmax = Math.Max(hist[xm, ym], hmax);
                    }

                    offsety += stride;
                }
            }

            var hk = 255.0 / Math.Sqrt(hmax);
            using (var hbitmap = new Bitmap(112, 112, PixelFormat.Format24bppRgb)) {
                for (var iy = 0; iy < 112; iy++) {
                    for (var ix = 0; ix < 112; ix++) {
                        var ib = (int)Math.Round(Math.Sqrt(hist[ix, iy]) * hk);
                        hbitmap.SetPixel(ix, iy, Color.FromArgb(ib, ib, ib));
                    }
                }

                hbitmap.Save("math.png", ImageFormat.Png);
            }

            return null;
        }
        */

        /*
        public static float GetDistance(float[] x, float[] y)
        {
            if (x.Length == 0 || y.Length == 0 || x.Length != y.Length) {
                return 1f;
            }

            var ldiff = x[0] - y[0];
            var adiff = x[1] - y[1];
            var bdiff = x[2] - y[2];

            var distance = (float)Math.Sqrt(ldiff * ldiff + adiff * adiff + bdiff * bdiff);
            return distance;
        }
        */
    }
}
