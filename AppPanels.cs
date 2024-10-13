using System.Drawing;
using ImageMagick;

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
            if (!AppImgs.TryGet(hash, out var img)) {
                return false;
            }

            var filename = AppFile.GetFileName(img.Name, AppConsts.PathHp);
            var imagedata = AppFile.ReadEncryptedFile(filename);
            if (imagedata == null) {
                return false;
            }

            var magickImage = AppBitmap.ImageDataToMagickImage(imagedata);
            if (magickImage == null) {
                return false;
            }

            var format = magickImage.Format.ToString().ToLower();
            var bitmap = AppBitmap.MagickImageToBitmap(magickImage, img.Orientation);
            if (bitmap != null) {
                if (AppVars.ShowXOR && idPanel == 1) {
                    using (var xb = new MagickImage())
                    using (var yb = new MagickImage()) {
                        xb.Read(_imgPanels[0].Bitmap);
                        yb.Read(bitmap);
                        AppBitmap.Composite(xb, yb, out var zb);
                        var bitmapxor = AppBitmap.MagickImageToBitmap(zb, RotateFlipType.RotateNoneFlipNone);
                        zb.Dispose();
                        bitmap.Dispose();
                        bitmap = bitmapxor;
                    }
                }

                var imgpanel = new ImgPanel(
                    hash: hash,
                    name: img.Name,
                    size: imagedata.LongLength,
                    bitmap: bitmap,
                    format: format);

                _imgPanels[idPanel] = imgpanel;
            }

            return true;
        }

        public static void SetVictim(int idPanel)
        {
            if (idPanel == 0 || idPanel == 1) {
                _imgPanels[idPanel].SetVictim();
            }
        }
    }
}
