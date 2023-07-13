using System;
using System.IO;

namespace ImgSoh
{
    public static class AppPanels
    {
        private static readonly ImgPanel[] _imgPanels = new ImgPanel[2];

        public static ImgPanel GetImgPanel(int idPanel)
        {
            return _imgPanels[idPanel];
        }

        public static bool SetImgPanel(int idPanel, string hash, string bin)
        {
            if (!AppImgs.TryGetValue(hash, out var img)) {
                return false;
            }

            var filename = img.GetFileName();
            var lastmodified = File.GetLastWriteTime(filename);
            var imagedata = FileHelper.ReadEncryptedFile(filename);
            if (imagedata == null) {
                return false;
            }

            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    var badname = Path.GetFileName(filename);
                    var badfilename = $"{AppConsts.PathGbProtected}\\{badname}{AppConsts.CorruptedExtension}";
                    if (File.Exists(badfilename)) {
                        FileHelper.DeleteToRecycleBin(badfilename, bin);
                    }

                    File.WriteAllBytes(badfilename, imagedata);
                    return false;
                }

                var format = magickImage.Format.ToString().ToLower();
                var datetaken = BitmapHelper.GetDateTaken(magickImage, lastmodified);
                var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, img.Orientation);
                if (bitmap != null) {
                    var blur = BitmapHelper.GetBlur(bitmap);
                    if (AppVars.ShowXOR && idPanel == 1 && _imgPanels[0].Bitmap.Width == bitmap.Width && _imgPanels[0].Bitmap.Height == bitmap.Height) {
                        var bitmapxor = BitmapHelper.BitmapXor(_imgPanels[0].Bitmap, bitmap);
                        bitmap.Dispose();
                        bitmap = bitmapxor;
                    }

                    var imgpanel = new ImgPanel(
                        img: img,
                        size: imagedata.LongLength,
                        bitmap: bitmap,
                        format: format,
                        dateTaken: datetaken,
                        blur: blur);

                    _imgPanels[idPanel] = imgpanel;
                }
            }

            return true;
        }


        public static void UpdateStatus(IProgress<string> progress)
        {
            var imgX = _imgPanels[0].Img;
            var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
            var shortFilename = imgX.GetShortFileName();
            var counters = AppImgs.GetCounters();
            progress?.Report($"{counters}: {shortFilename} [{age} ago]");
        }
    }
}
