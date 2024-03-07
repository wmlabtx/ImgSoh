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
            if (!AppDatabase.TryGetImg(hash, out var img)) {
                return false;
            }

            var filename = Helper.GetFileName(img.Folder, hash);
            //var lastmodified = File.GetLastWriteTime(filename);
            var imagedata = FileHelper.ReadEncryptedFile(filename);
            if (imagedata == null) {
                return false;
            }

            var fingerPrint = ExifHelper.GetFingerPrint(imagedata);
            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    return false;
                }

                var format = magickImage.Format.ToString().ToLower();
                //var datetaken =  BitmapHelper.GetDateTaken(magickImage, lastmodified);
                var datetaken = ExifHelper.GetDateTaken(fingerPrint);
                var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, img.Orientation);
                if (bitmap != null) {
                    if (AppVars.ShowXOR && idPanel == 1 && _imgPanels[0].Bitmap.Width == bitmap.Width && _imgPanels[0].Bitmap.Height == bitmap.Height) {
                        var bitmapxor = BitmapHelper.BitmapXor(_imgPanels[0].Bitmap, bitmap);
                        bitmap.Dispose();
                        bitmap = bitmapxor;
                    }

                    var imgpanel = new ImgPanel(
                        hash: hash,
                        size: imagedata.LongLength,
                        bitmap: bitmap,
                        format: format,
                        fingerPrint: fingerPrint,
                        dateTaken: datetaken);

                    _imgPanels[idPanel] = imgpanel;
                }
            }

            return true;
        }
    }
}
