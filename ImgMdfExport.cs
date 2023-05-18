using System;
using System.IO;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Export(int idpanel, IProgress<string> progress)
        {
            var img = AppPanels.GetImgPanel(idpanel).Img;
            var filename = img.GetFileName();
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
                    var shortfilename = img.GetShortFileName();
                    progress?.Report($"Bad {shortfilename}");
                }
            }
        }
    }
}
