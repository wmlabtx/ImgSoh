using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace ImgSoh
{
    public static class VitHelper
    {
        private static Net _net;

        public static void LoadNet(IProgress<string> progress)
        {
            progress?.Report($"Loading net{AppConsts.CharEllipsis}");
            //_net = CvDnn.ReadNetFromOnnx(AppConsts.FileVit);
            _net = CvDnn.ReadNet(AppConsts.FileVit);
        }

        public static IEnumerable<string> GetInfo()
        {
            return _net.GetLayerNames();
        }

        private static Mat BitmapToMat(Bitmap bitmap)
        {
            var input = new Mat(new[] { 1, 3, 224, 224 }, MatType.CV_32F);
            using (var b = BitmapHelper.ScaleAndCut(bitmap, 224, 16)) {
                //b.Save("bitmap.png", ImageFormat.Png);
                var bitmapdata = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
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

                        var red = (rbyte / 255f - 0.5f) / 0.5f;
                        var green = (gbyte / 255f - 0.5f) / 0.5f;
                        var blue = (bbyte / 255f - 0.5f) / 0.5f;

                        input.At<float>(0, 0, y, x) = red;
                        input.At<float>(0, 1, y, x) = green;
                        input.At<float>(0, 2, y, x) = blue;
                    }

                    offsety += stride;
                }
            }

            return input;
        }

        public static float[] CalculateFloatVector(Bitmap bitmap)
        {
            float[] vector;
            using (var input = BitmapToMat(bitmap)) {
                _net.SetInput(input);
                var output = _net.Forward("logits");
                output.GetArray(out vector);
            }

            return vector;
        }

        public static float GetDistance(float[] x, float[] y)
        {
            if (x.Length == 0 || y.Length == 0 || x.Length != y.Length) {
                return 1f;
            }

            var dot = 0.0;
            var magx = 0.0;
            var magy = 0.0;
            for (var n = 0; n < x.Length; n++) {
                dot += x[n] * y[n];
                magx += x[n] * x[n];
                magy += y[n] * y[n];
            }

            return 1f - (float)(dot / (Math.Sqrt(magx) * Math.Sqrt(magy)));
        }
    }
}
