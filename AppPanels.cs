using System;
using System.Drawing;
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

        public static bool SetImgPanel(int idPanel, string hash)
        {
            if (!AppDatabase.TryGetImgFolderOrientationLastView(hash, out var folder, out var orientation, out var lastView)) {
                return false;
            }

            var filename = Helper.GetFileName(folder, hash);
            var lastmodified = File.GetLastWriteTime(filename);
            var imagedata = FileHelper.ReadEncryptedFile(filename);
            if (imagedata == null) {
                return false;
            }

            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    return false;
                }

                var format = magickImage.Format.ToString().ToLower();
                var datetaken = BitmapHelper.GetDateTaken(magickImage, lastmodified);
                var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, orientation);
                if (bitmap != null) {
                    if (AppVars.ShowXOR && idPanel == 1 && _imgPanels[0].Bitmap.Width == bitmap.Width && _imgPanels[0].Bitmap.Height == bitmap.Height) {
                        var bitmapxor = BitmapHelper.BitmapXor(_imgPanels[0].Bitmap, bitmap);
                        bitmap.Dispose();
                        bitmap = bitmapxor;
                    }

                    var imgpanel = new ImgPanel(
                        hash: hash,
                        folder: folder,
                        lastView: lastView,
                        size: imagedata.LongLength,
                        bitmap: bitmap,
                        format: format,
                        dateTaken: datetaken);

                    _imgPanels[idPanel] = imgpanel;
                }
            }

            return true;
        }
    }
}
