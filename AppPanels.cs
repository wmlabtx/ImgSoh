using System;
using System.Collections.Generic;
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

        private static int _position;
        private static List<string> _similars;

        public static void SetSimilars(List<string> similars, IProgress<string> progress, string bin)
        {
            _similars = similars;
            SetFirstPosition(progress, bin);
        }

        public static string GetRightName()
        {
            var hash = _similars[_position];
            return hash;
        }

        public static void SetFirstPosition(IProgress<string> progress, string bin)
        {
            _position = 0;
            while (!SetImgPanel(1, _similars[_position], bin)) {
                _similars.RemoveAt(0);
            }

            UpdateStatus(progress);
        }

        public static void MoveRightPosition(IProgress<string> progress, string bin)
        {
            while (_position < _similars.Count - 1) {
                _position++;
                if (SetImgPanel(1, _similars[_position], bin)) {
                    UpdateStatus(progress);
                    break;
                }
            }
        }

        public static void MoveLeftPosition(IProgress<string> progress, string bin)
        {
            while (_position > 0) {
                _position--;
                if (SetImgPanel(1, _similars[_position], bin)) {
                    UpdateStatus(progress);
                    break;
                }
            }
        }

        private static void UpdateStatus(IProgress<string> progress)
        {
            var imgX = _imgPanels[0].Img;
            var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
            var similarsFound = _similars.Count;
            var shortFilename = imgX.GetShortFileName();
            var counters = AppImgs.GetCounters();
            progress?.Report($"{counters}: {shortFilename} [{age} ago] = ({_position}/{similarsFound})");
        }
    }
}
