using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using ImageMagick;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ImgSoh
{
    public static class BitmapHelper
    {
        public static System.Windows.Media.ImageSource ImageSourceFromBitmap(Bitmap bitmap)
        {
            if (bitmap == null) {
                return null;
            }

            var handle = bitmap.GetHbitmap();
            try {
                var image = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                image.Freeze();
                return image;
            }
            finally {
                NativeMethods.DeleteObject(handle);
            }
        }

        public static MagickImage ImageDataToMagickImage(byte[] data)
        {
            MagickImage magickImage;
            try {
                magickImage = new MagickImage(data);
            }
            catch (MagickMissingDelegateErrorException) {
                magickImage = null;
            }
            catch (MagickCorruptImageErrorException) {
                magickImage = null;
            }

            return magickImage;
        }

        public static Bitmap MagickImageToBitmap(MagickImage magickImage, RotateFlipType rft)
        {
            var bitmap = magickImage.ToBitmap();
            if (bitmap.PixelFormat == PixelFormat.Format24bppRgb) {
                bitmap.RotateFlip(rft);
                return bitmap;
            }

            var bitmap24BppRgb = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bitmap24BppRgb)) {
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            }

            bitmap24BppRgb.RotateFlip(rft);
            return bitmap24BppRgb;
        }

        public static string GetRecommendedExt(MagickImage image)
        {
            switch (image.Format) {
                case MagickFormat.Jpeg:
                    return ".jpg";
                case MagickFormat.Png:
                    return ".png";
                case MagickFormat.Bmp:
                    return ".bmp";
                case MagickFormat.Gif:
                    return ".gif";
                case MagickFormat.WebP:
                    return ".webp";
                case MagickFormat.Heic:
                    return ".heic";
                default:
                    throw new Exception($"Unkown extension for {image.Format}");
            }
        }

        public static bool BitmapToImageData(Bitmap bitmap, MagickFormat magickFormat, out byte[] imagedata)
        {
            try {
                var mf = new MagickFactory();
                using (var image = new MagickImage(mf.Image.Create(bitmap))) {
                    image.Format = magickFormat;
                    using (var ms = new MemoryStream()) {
                        image.Write(ms);
                        imagedata = ms.ToArray();
                        return true;
                    }
                }
            }
            catch (MagickException) {
                imagedata = null;
                return false;
            }
        }

        public static DateTime GetDateTaken(MagickImage magickImage, DateTime defaultValue)
        {
            var exif = magickImage.GetExifProfile();
            if (exif == null) {
                return defaultValue;
            }

            var possibleExifTags = new[] { ExifTag.DateTimeOriginal, ExifTag.DateTimeDigitized, ExifTag.DateTime };
            foreach (var tag in possibleExifTags) {
                var field = exif.GetValue(tag);
                if (field == null) {
                    continue;
                }

                var value = field.ToString();
                if (!DateTime.TryParseExact(value, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) {
                    continue;
                }

                return dt;
            }

            return defaultValue;
        }

        public static Bitmap ScaleAndCut(Bitmap bitmap, int dim, int border)
        {
            Bitmap bitmapdim;
            var bigdim = dim + (border * 2);
            int width;
            int height;
            if (bitmap.Width >= bitmap.Height) {
                height = bigdim;
                width = (int)Math.Round(bitmap.Width * bigdim / (float)bitmap.Height);
            }
            else {
                width = bigdim;
                height = (int)Math.Round(bitmap.Height * bigdim / (float)bitmap.Width);
            }

            using (Bitmap bitmapbigdim = new Bitmap(width, height, PixelFormat.Format24bppRgb)) {
                using (var g = Graphics.FromImage(bitmapbigdim)) {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bitmap, 0, 0, width, height);
                }

                int x;
                int y;
                if (width >= height) {
                    x = border + ((width - height) / 2);
                    y = border;
                }
                else {
                    x = border;
                    y = border + ((height - width) / 2);
                }

                bitmapdim = bitmapbigdim.Clone(new Rectangle(x, y, dim, dim), PixelFormat.Format24bppRgb);
            }

            return bitmapdim;
        }

        public static Bitmap BitmapXor(Bitmap xb, Bitmap yb)
        {
            var xd = xb.LockBits(new Rectangle(0, 0, xb.Width, xb.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] xa = new byte[xd.Stride * xd.Height];
            Marshal.Copy(xd.Scan0, xa, 0, xa.Length);
            xb.UnlockBits(xd);

            var yd = yb.LockBits(new Rectangle(0, 0, yb.Width, yb.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] ya = new byte[yd.Stride * yd.Height];
            Marshal.Copy(yd.Scan0, ya, 0, ya.Length);
            yb.UnlockBits(yd);

            for (var i = 0; i < xa.Length - 2; i += 3) {
                ya[i] = (byte)Math.Min(255, Math.Abs(xa[i] - ya[i]) << 3);
                ya[i + 1] = (byte)Math.Min(255, Math.Abs(xa[i + 1] - ya[i + 1]) << 2);
                ya[i + 2] = (byte)Math.Min(255, Math.Abs(xa[i + 2] - ya[i + 2]) << 2);
                if (ya[i] == 255 && ya[i + 1] == 255 && ya[i + 2] == 255) {
                    Bitmap cb = new Bitmap(yb);
                    return cb;
                }
            }

            Bitmap zb = new Bitmap(yb);
            var zd = zb.LockBits(new Rectangle(0, 0, zb.Width, zb.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            Marshal.Copy(ya, 0, zd.Scan0, ya.Length);
            zb.UnlockBits(zd);

            return zb;
        }

        public static double GetBlur(Bitmap x)
        {
            double result;
            using (var bitmap = ScaleAndCut(x, 512, 16))
            using (var cvColor = bitmap.ToMat())
            using (var cvGray = new Mat())
            using (var lap = new Mat())
            using (var mu = new Mat())
            using (var sigma = new Mat()) {
                Cv2.CvtColor(cvColor, cvGray, ColorConversionCodes.BGR2GRAY);
                Cv2.Laplacian(cvGray, lap, MatType.CV_64F);
                Cv2.MeanStdDev(cvGray, mu, sigma);
                sigma.GetArray(out double[] array);
                result = array[0] * array[0];
            }

            return result;
        }

        private static double CubeRoot(double val)
        {
            return Math.Pow(val, 1.0 / 3.0);
        }

        private static double Fn(int val)
        {
            var abs = val / 255.0;
            return abs < 0.04045 ?
                abs / 12.92 :
                Math.Pow((abs + 0.055) / 1.055, 2.4);
        }

        public static void RGB2LAB(int rb, int gb, int bb, out double l, out double a, out double b)
        {
            var rd = Fn(rb);
            var gd = Fn(gb);
            var bd = Fn(bb);

            var li = CubeRoot((0.41222147079999993 * rd) + (0.5363325363 * gd) + (0.0514459929 * bd));
            var mi = CubeRoot((0.2119034981999999 * rd) + (0.6806995450999999 * gd) + (0.1073969566 * bd));
            var si = CubeRoot((0.08830246189999998 * rd) + (0.2817188376 * gd) + (0.6299787005000002 * bd));

            l = (0.2104542553 * li) + (0.793617785 * mi) - (0.0040720468 * si);
            a = 1.9779984951 * li - (2.428592205 * mi) + (0.4505937099 * si);
            b = (0.0259040371 * li) + (0.7827717662 * mi) - (0.808675766 * si);
        }

        /*

// correlary of first psuedocode block here (f_inv) : https://bottosson.github.io/posts/colorwrong/#what-can-we-do%3F ; "applying the inverse of the sRGB nonlinear transform function.." -- keeping the abbreviated syntax of arrow functions and ? : if/then, despite that they confuse and stretch my noob brain:
const gammaToLinear = (c) =>
  c >= 0.04045 ? Math.pow((c + 0.055) / 1.055, 2.4) : c / 12.92;
// correlary of the first " : "..then switching back" :
const linearToGamma = (c) =>
  c >= 0.0031308 ? 1.055 * Math.pow(c, 1 / 2.4) - 0.055 : 12.92 * c;

// Lab coordinates (parameters):
// L = Lightness (0 (black) to ?? (diffuse white)
// a = green (at negative -- is there a lower bound?) to red (positive)
// b = blue (at negative) to yellow (at positive).
// You can manually construct an object literal to pass to this function this way:
// labVals = {L: 0.75, a: 0.7, b: 0.2};
// sRGBresultObjectLiteral = oklabToSRGB(labVals);
// You can also construct that as just {0.75, 0.7, 0.2}, and still pass it and it will work, apparently
// "..Oklab is represented as an object {L, a, b} where L is between 0 and 1 for normal SRGB colors. a and b have a less clearly defined range, but will normally be between -0.5 and +0.5. Neutral gray colors will have a and b at zero (or very close)." re: https://www.npmjs.com/package/oklab
// numbers updated from C++ example at https://bottosson.github.io/posts/oklab/ as updated 2021-01-25
// helpful references:
// https://observablehq.com/@sebastien/srgb-rgb-gamma
// Other references: https://matt77hias.github.io/blog/2018/07/01/linear-gamma-and-sRGB-color-spaces.html
// Takeaway: before manipulating color for sRGB (gamma-corrected or gamma compressed), convert it to linear RGB or a linear color space. Then do the manipulation, then convert it back to sRGB.
function rgbToOklab({r, g, b}) {
  // This is my undersanding: JavaScript canvas and many other virtual and literal devices use gamma-corrected (non-linear lightness) RGB, or sRGB. To convert sRGB values for manipulation in the Oklab color space, you must first convert them to linear RGB. Where Oklab interfaces with RGB it expects and returns linear RGB values. This next step converts (via a function) sRGB to linear RGB for Oklab to use:
  r = gammaToLinear(r / 255); g = gammaToLinear(g / 255); b = gammaToLinear(b / 255);
  // This is the Oklab math:
  var l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b;
  var m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b;
  var s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b;
  // Math.crb (cube root) here is the equivalent of the C++ cbrtf function here: https://bottosson.github.io/posts/oklab/#converting-from-linear-srgb-to-oklab
  l = Math.cbrt(l); m = Math.cbrt(m); s = Math.cbrt(s);
  return {
    L: l * +0.2104542553 + m * +0.7936177850 + s * -0.0040720468,
    a: l * +1.9779984951 + m * -2.4285922050 + s * +0.4505937099,
    b: l * +0.0259040371 + m * +0.7827717662 + s * -0.8086757660
  }
}

function oklabToSRGB({L, a, b}) {
  var l = L + a * +0.3963377774 + b * +0.2158037573;
  var m = L + a * -0.1055613458 + b * -0.0638541728;
  var s = L + a * -0.0894841775 + b * -1.2914855480;
  // The ** operator here cubes; same as l_*l_*l_ in the C++ example:
  l = l ** 3; m = m ** 3; s = s ** 3;
  var r = l * +4.0767416621 + m * -3.3077115913 + s * +0.2309699292;
  var g = l * -1.2684380046 + m * +2.6097574011 + s * -0.3413193965;
  var b = l * -0.0041960863 + m * -0.7034186147 + s * +1.7076147010;
  // Convert linear RGB values returned from oklab math to sRGB for our use before returning them:
  r = 255 * linearToGamma(r); g = 255 * linearToGamma(g); b = 255 * linearToGamma(b);
  // OPTION: clamp r g and b values to the range 0-255; but if you use the values immediately to draw, JavaScript clamps them on use:
  r = clamp(r, 0, 255); g = clamp(g, 0, 255); b = clamp(b, 0, 255);
  // OPTION: round the values. May not be necessary if you use them immediately for rendering in JavaScript, as JavaScript (also) discards decimals on render:
    r = Math.round(r); g = Math.round(g); b = Math.round(b);
  return {r, g, b};
}
         
         */
    }
}
