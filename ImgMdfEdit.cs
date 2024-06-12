using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Policy;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Rotate(string hash, RotateFlipType rft)
        {
            if (!AppDatabase.TryGetImg(hash, out var imgR)) {
                return;
            }

            var filename = Helper.GetFileName(imgR.Path, hash, imgR.Ext);
            var imagedata = FileHelper.ReadFile(filename);
            if (imagedata == null) {
                return;
            }

            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    return;
                }

                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, rft)) {
                    if (bitmap == null) {
                        return;
                    }

                    var rvector = VitHelper.CalculateVector(bitmap).ToArray();
                    AppDatabase.SetVector(hash, rvector);
                    AppDatabase.SetOrientation(hash, rft);
                }
            }
        }

        public static void Move(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            var hashY = AppPanels.GetImgPanel(1 - idpanel).Hash;
            if (AppDatabase.TryGetImg(hashX, out var imgX) && AppDatabase.TryGetImg(hashY, out var imgY)) {
                if (imgX.Path.Equals(imgY.Path)) {
                    return;
                }

                var filenameSource = Helper.GetFileName(imgX.Path, hashX, imgX.Ext);
                var filenameDestination = Helper.GetFileName(imgY.Path, hashX, imgX.Ext);
                File.Move(filenameSource, filenameDestination);
                AppDatabase.SetPath(hashX, imgY.Path);
            }
        }
    }
}
