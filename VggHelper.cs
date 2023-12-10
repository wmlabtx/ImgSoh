﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace ImgSoh
{
    public static class VggHelper
    {
        private static Net _net;

        public static void LoadNet(IProgress<string> progress)
        {
            progress?.Report($"Loading net{AppConsts.CharEllipsis}");
            _net = CvDnn.ReadNetFromOnnx(AppConsts.FileVgg);
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

                        var red = (rbyte / 255f - 0.485f) / 0.229f;
                        var green = (gbyte / 255f - 0.456f) / 0.224f;
                        var blue = (bbyte / 255f - 0.406f) / 0.225f;

                        input.At<float>(0, 0, y, x) = red;
                        input.At<float>(0, 1, y, x) = green;
                        input.At<float>(0, 2, y, x) = blue;
                    }

                    offsety += stride;
                }
            }

            return input;
        }

        public static byte[] CalculateVector(Bitmap bitmap)
        {
            byte[] vector;
            using (var input = BitmapToMat(bitmap)) {
                _net.SetInput(input);
                //var output = _net.Forward("onnx_node!resnetv27_flatten0_reshape0");
                var output = _net.Forward("onnx_node!flatten_70");
                output.GetArray(out float[] buffer);
                vector = new byte[buffer.Length];
                for (var i = 0; i < buffer.Length; i++) {
                    vector[i] = (byte)Math.Min(255, (int)(buffer[i] * 255.0 / 10.0));
                }
            }

            return vector;
        }

        public static IEnumerable<float> CalculateFloatVector(Bitmap bitmap)
        {
            float[] vector;
            using (var input = BitmapToMat(bitmap)) {
                _net.SetInput(input);
                var output = _net.Forward("onnx_node!flatten_70");
                output.GetArray(out vector);
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

        public static float GetSpearmanDistance(byte[] x, byte[] y)
        {
            if (x.Length == 0 || y.Length == 0 || x.Length != y.Length) {
                return 1f;
            }

            var xr = getSpearmanRank(x);
            var yr = getSpearmanRank(y);
            var distance = getSpearmanCorrelation(xr, yr);
            return distance;
        }

        private static float[] getSpearmanRank(byte[] x)
        {
            var n = x.Length;
            var result = new float[n];
            for (var i = 0; i < n; i++) {
                result[i] = 0f;
                int r = 1, s = 1;
                for (var j = 0; j < i; j++) {
                    if (x[j] < x[i]) {
                        r++;
                    }

                    if (x[j] == x[i]) {
                        s++;
                    }
                }

                for (var j = i + 1; j < n; j++) {
                    if (x[j] < x[i]) {
                        r++;
                    }

                    if (x[j] == x[i]) {
                        s++;
                    }
                }

                result[i] = (r + (s - 1) * 0.5f);
            }

            return result;
        }

        private static float getSpearmanCorrelation(float[] x, float[] y)
        {
            var n = x.Length;
            float sum_X = 0f, sum_Y = 0f, sum_XY = 0f;
            float squareSum_X = 0f, squareSum_Y = 0f;

            for (var i = 0; i < n; i++) {
                sum_X += x[i];
                sum_Y += y[i];
                sum_XY += x[i] * y[i];
                squareSum_X += x[i] * x[i];
                squareSum_Y += y[i] * y[i];
            }

            var result = 1f - (float)((n * sum_XY - sum_X * sum_Y) / Math.Sqrt((n * squareSum_X - sum_X * sum_X) * (n * squareSum_Y - sum_Y * sum_Y)));
            return result;
        }
    }
}
