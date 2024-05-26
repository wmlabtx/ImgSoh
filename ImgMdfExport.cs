using System;
using System.IO;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Export(int idpanel, IProgress<string> progress)
        {
            var hash = AppPanels.GetImgPanel(idpanel).Hash;
            if (AppDatabase.TryGetImg(hash, out var img)) {
                var folder = img.Folder;
                var filename = Helper.GetFileName(folder, hash);
                var imagedata = FileHelper.ReadFile(filename);
                if (imagedata == null) {
                    return;
                }

                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                    if (magickImage != null) {
                        var ext = magickImage.Format.ToString().ToLower();
                        var name = Path.GetFileNameWithoutExtension(filename);
                        var exportfilename = $"{AppConsts.PathRwProtected}\\{name}.{ext}";
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

        public static void Move(string destination, IProgress<string> progress)
        {
            var hash = AppPanels.GetImgPanel(0).Hash;
            if (AppDatabase.TryGetImg(hash, out var img)) {
                var folder = img.Folder;
                var filename = Helper.GetFileName(folder, hash);
                var shortfilename = Helper.GetShortFileName(folder, hash);
                var imagedata = FileHelper.ReadEncryptedFile(filename);
                if (imagedata != null) {
                    using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                        if (magickImage != null) {
                            var ext = magickImage.Format.ToString().ToLower();
                            var name = Path.GetFileNameWithoutExtension(filename);
                            var exportfilename = $"{destination}\\{name}.{ext}";
                            File.WriteAllBytes(exportfilename, imagedata);
                            if (img.Taken != DateTime.MinValue) {
                                File.SetLastWriteTime(exportfilename, img.Taken);
                            }

                            progress?.Report($"Exported {exportfilename}");
                            AppDatabase.SetDeleted(hash);
                            File.Delete(filename);
                            ConfirmOpposite(1);
                        }
                        else {
                            progress?.Report($"Bad {shortfilename}");
                        }
                    }
                }
                else {
                    progress?.Report($"Cannot read {shortfilename}");
                }
            }
        }
    }
}
