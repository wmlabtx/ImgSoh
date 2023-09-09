using System;
using System.Drawing;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Rotate(string hash, RotateFlipType rft, IProgress<string> progress)
        {
            if (!AppDatabase.TryGetImg(hash, out var img)) {
                progress.Report($"Image {hash} not found");
                return;
            }

            var folder = img.Folder;
            var filename = Helper.GetFileName(folder, hash);
            var shortfilename = Helper.GetShortFileName(folder, hash);
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

                    AppDatabase.ImgUpdateOrientation(hash, rft);
                    var rvector = VggHelper.CalculateVector(bitmap);
                    AppDatabase.ImgUpdateVector(hash, rvector);
                }
            }
        }
    }
}
