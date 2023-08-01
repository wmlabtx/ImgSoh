using System;
using System.IO;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Export(int idpanel, IProgress<string> progress)
        {
            var hash = AppPanels.GetImgPanel(idpanel).Hash;
            if (AppDatabase.TryGetImgFolder(hash, out var folder)) {
                var filename = Helper.GetFileName(folder, hash);
                var imagedata = FileHelper.ReadFile(filename);
                if (imagedata == null) {
                    return;
                }

                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                    if (magickImage != null) {
                        var ext = magickImage.Format.ToString().ToLower();
                        var name = Path.GetFileNameWithoutExtension(filename);
                        var exportfilename = $"{AppConsts.PathRw}\\{name}.{ext}";
                        File.WriteAllBytes(exportfilename, imagedata);
                        progress?.Report($"Exported {exportfilename}");
                    }
                    else {
                        var shortfilename = Helper.GetShortFileName(folder, hash);
                        progress?.Report($"Bad {shortfilename}");
                    }
                }
            }
        }
    }
}
