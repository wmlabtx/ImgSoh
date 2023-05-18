using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ImgSoh
{
    public static class LabHelper
    {
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
    }
}
