using System.Drawing;
using System.Linq;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Rotate(string hash, RotateFlipType rft)
        {
            if (!AppImgs.TryGet(hash, out var img)) {
                return;
            }

            var filename = AppFile.GetFileName(img.Name, AppConsts.PathHp);
            var imagedata = AppFile.ReadEncryptedFile(filename);
            if (imagedata == null) {
                return;
            }

            using (var magickImage = AppBitmap.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    return;
                }

                using (var bitmap = AppBitmap.MagickImageToBitmap(magickImage, rft)) {
                    if (bitmap == null) {
                        return;
                    }

                    var rvector = AppVit.CalculateVector(bitmap).ToArray();
                    var rmagnitude = AppVit.GetMagnitude(rvector);
                    AppImgs.SetVector(hash, rvector);
                    AppImgs.SetMagnitude(hash, rmagnitude);
                    AppImgs.SetOrientation(hash, rft);
                }
            }
        }
    }
}
