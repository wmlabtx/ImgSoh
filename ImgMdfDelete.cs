using System;
using System.IO;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        private static void Delete(string hashD, string suffix, IProgress<string> progress)
        {
            if (AppDatabase.TryGetImg(hashD, out var imgD)) {
                if (!imgD.Deleted) {
                    var folderD = imgD.Folder;
                    var shortname = Helper.GetShortFileName(folderD, hashD);
                    progress?.Report($"Delete {shortname}");
                    AppDatabase.SetDeleted(hashD);
                    var filename = Helper.GetFileName(folderD, hashD);
                    DeleteFile(filename, suffix);
                }
            }
        }

        private static void DeleteFile(string filename, string suffix)
        {
            var name = Path.GetFileNameWithoutExtension(filename).ToLower();
            var extension = Path.GetExtension(filename);
            byte[] imagedata;
            if (extension.Equals(AppConsts.DatExtension, StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(AppConsts.MzxExtension, StringComparison.OrdinalIgnoreCase)) {
                imagedata = File.ReadAllBytes(filename);
                
                var decrypteddata = extension.Equals(AppConsts.DatExtension, StringComparison.OrdinalIgnoreCase) ?
                    EncryptionHelper.DecryptDat(imagedata, name) :
                    EncryptionHelper.Decrypt(imagedata, name);

                if (decrypteddata != null) {
                    imagedata = decrypteddata;
                }
            }
            else {
                imagedata = File.ReadAllBytes(filename);
            }

            using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                if (magickImage != null) {
                    extension = magickImage.Format.ToString().ToLower();
                }
            }

            var now = DateTime.Now;
            File.SetAttributes(filename, FileAttributes.Normal);
            string deletedFilename;
            var counter = 0;
            do {
                deletedFilename = $"{AppConsts.PathGbProtected}\\{now.Year}-{now.Month:D2}-{now.Day:D2}\\{now.Hour:D2}{now.Minute:D2}{now.Second:D2}{suffix}.{name}";
                if (counter > 0) {
                    deletedFilename += $"({counter})";
                }

                deletedFilename += $".{extension}";
                counter++;
            }
            while (File.Exists(deletedFilename));
            FileHelper.WriteFile(deletedFilename, imagedata);
            File.Delete(filename);
        }

        public static void Delete(int idpanel, string suffix, IProgress<string> progress)
        {
            var hashD = AppPanels.GetImgPanel(idpanel).Hash;
            Delete(hashD, suffix, progress);
            ConfirmOpposite(1 - idpanel);
        }
    }
} 