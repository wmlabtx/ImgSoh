using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows;
using ImageMagick;

namespace ImgSoh
{
    public static class AppBitmap
    {
        private static readonly ExifInfo _exifinfo = new ExifInfo();

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

            using (var bitmapbigdim = new Bitmap(width, height, PixelFormat.Format24bppRgb)) {
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

        public static void Composite(MagickImage xb, MagickImage yb, out MagickImage zb)
        {
            var width = yb.Width;
            var height = yb.Height;
            xb.Grayscale();
            xb.Resize(512, 512);
            yb.Grayscale();
            yb.Resize(512, 512);

            zb = new MagickImage(xb);
            zb.ColorFuzz = new Percentage(50);
            zb.Composite(yb, CompositeOperator.Difference);
            var settings = new MorphologySettings {
                Method = MorphologyMethod.Dilate,
                Kernel = Kernel.Disk,
                 Iterations = 1
            };

            zb.Morphology(settings);
            zb.Resize(width, height);
        }

        public static void Read(string filename, out DateTime taken, out int meta)
        {
            _exifinfo.Read(filename);
            taken = _exifinfo.Taken;
            meta = _exifinfo.Items.Length;
        }

        public static void StopExif()
        {
            _exifinfo.Stop();
        }
    }
}
