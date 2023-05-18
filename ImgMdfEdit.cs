using System;
using System.Drawing;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Rotate(string hash, RotateFlipType rft, IProgress<string> progress)
        {
            if (!AppImgs.TryGetValue(hash, out var img)) {
                progress.Report($"Image {hash} not found");
                return;
            }

            var filename = img.GetFileName();
            var shortfilename = img.GetShortFileName();
            var imagedata = FileHelper.ReadEncryptedFile(filename);
            if (imagedata == null) {
                progress.Report($"Cannot read {shortfilename}");
                return;
            }

            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage == null) {
                    progress.Report($"Corrupted image {shortfilename}");
                    return;
                }

                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, rft)) {
                    if (bitmap == null) {
                        progress.Report($"Corrupted image {shortfilename}");
                        return;
                    }

                    img.SetOrientation(rft);
                    var rvector = VggHelper.CalculateVector(bitmap);
                    img.SetVector(rvector);
                }
            }
        }
    }
}
