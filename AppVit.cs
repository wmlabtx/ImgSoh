using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ImgSoh
{
    public static class AppVit
    {
        private static InferenceSession _session;

        public static void LoadNet(IProgress<string> progress)
        {
            progress?.Report($"Loading net{AppConsts.CharEllipsis}");
            _session = new InferenceSession(AppConsts.FileVit);
        }

        public static IEnumerable<float> CalculateVector(Bitmap bitmap)
        {
            var tensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });
            using (var b = AppBitmap.ScaleAndCut(bitmap, 224, 16)) {
                var bitmapdata = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
                var stride = bitmapdata.Stride;
                var data = new byte[Math.Abs(bitmapdata.Stride * bitmapdata.Height)];
                Marshal.Copy(bitmapdata.Scan0, data, 0, data.Length);
                b.UnlockBits(bitmapdata);
                var width = b.Width;
                Parallel.For(0, b.Height, y => {
                    var offsetx = y * stride;
                    for (var x = 0; x < width; x++) {
                        var rbyte = data[offsetx + 2];
                        var gbyte = data[offsetx + 1];
                        var bbyte = data[offsetx];
                        offsetx += 3;

                        /*
                         for gray model
                        var gray = 0.299f * rbyte + 0.587f * gbyte + 0.114f * bbyte;
                        var red = (gray / 255f - 0.5f) / 0.5f;
                        tensor[0, 0, y, x] = red;
                        tensor[0, 1, y, x] = red;
                        tensor[0, 2, y, x] = red;
                        */

                        var red = (rbyte / 255f - 0.5f) / 0.5f;
                        var green = (gbyte / 255f - 0.5f) / 0.5f;
                        var blue = (bbyte / 255f - 0.5f) / 0.5f;
                        tensor[0, 0, y, x] = red;
                        tensor[0, 1, y, x] = green;
                        tensor[0, 2, y, x] = blue;
                    }
                });
            }

            var container = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("pixel_values", tensor) };
            var results = _session.Run(container);
            return results[0].AsEnumerable<float>();
        }

        public static float GetMagnitude(IEnumerable<float> x)
        {
            var sumx = x.Aggregate(0.0, (current, e) => current + e * e);
            return (float)Math.Sqrt(sumx);
        }

        public static float GetDistance(IEnumerable<float> x, float magx, IEnumerable<float> y, float magy)
        {
            if (magx < 0f || magy < 0f) {
                return 2f;
            }

            var dot = x.Zip(y, (a, b) => a * b).Sum();
            return 1f - dot / (magx * magy);
        }
    }
}
