using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using ImageMagick;

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
            catch (MagickCoderErrorException) {
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
            var xa = new byte[xd.Stride * xd.Height];
            Marshal.Copy(xd.Scan0, xa, 0, xa.Length);
            xb.UnlockBits(xd);

            var yd = yb.LockBits(new Rectangle(0, 0, yb.Width, yb.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] ya = new byte[yd.Stride * yd.Height];
            Marshal.Copy(yd.Scan0, ya, 0, ya.Length);
            yb.UnlockBits(yd);

            for (var i = 0; i < xa.Length - 2; i += 3) {
                ya[i] = (byte)Math.Min(255, Math.Abs(xa[i] - ya[i]) << 3);
                ya[i + 1] = (byte)Math.Min(255, Math.Abs(xa[i + 1] - ya[i + 1]) << 1);
                ya[i + 2] = (byte)Math.Min(255, Math.Abs(xa[i + 2] - ya[i + 2]) << 1);
            }

            var zb = new Bitmap(yb);
            var zd = zb.LockBits(new Rectangle(0, 0, zb.Width, zb.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            Marshal.Copy(ya, 0, zd.Scan0, ya.Length);
            zb.UnlockBits(zd);

            return zb;
        }
    }
}
