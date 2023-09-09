using System;
using System.IO;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        private static void Delete(string hashD, IProgress<string> progress)
        {
            if (AppDatabase.TryGetImg(hashD, out var imgD)) {
                var folderD = imgD.Folder;
                var shortname = Helper.GetShortFileName(folderD, hashD);
                progress.Report($"Delete {shortname}");
                var filename = Helper.GetFileName(folderD, hashD);
                var imagedata = FileHelper.ReadEncryptedFile(filename);
                if (imagedata != null) {
                    using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
                        if (magickImage != null) {
                            var extension = magickImage.Format.ToString().ToLower();
                            var now = DateTime.Now;
                            File.SetAttributes(filename, FileAttributes.Normal);
                            var name = Path.GetFileNameWithoutExtension(filename);
                            string deletedFilename;
                            var counter = 0;
                            do {
                                deletedFilename = counter == 0 ?
                                    $"{AppConsts.PathGbProtected}\\{now.Year}-{now.Month:D2}-{now.Day:D2}\\{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.{name}.{extension}" :
                                    $"{AppConsts.PathGbProtected}\\{now.Year}-{now.Month:D2}-{now.Day:D2}\\{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.{name}({counter}).{extension}";

                                counter++;
                            }
                            while (File.Exists(deletedFilename));
                            FileHelper.WriteFile(deletedFilename, imagedata);
                        }
                    }
                }

                AppDatabase.DeleteImg(hashD);
                File.Delete(filename);
            }
        }

        private static void DeleteFile(string filename)
        {
            var now = DateTime.Now;
            File.SetAttributes(filename, FileAttributes.Normal);
            var name = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);
            string deletedFilename;
            var counter = 0;
            do {
                deletedFilename = counter == 0 ?
                    $"{AppConsts.PathGbProtected}\\{now.Year}-{now.Month:D2}-{now.Day:D2}\\{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.{name}.{extension}" :
                    $"{AppConsts.PathGbProtected}\\{now.Year}-{now.Month:D2}-{now.Day:D2}\\{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.{name}({counter}).{extension}";

                counter++;
            }
            while (File.Exists(deletedFilename));
            File.Move(filename, deletedFilename);
        }

        public static void Delete(int idpanel, IProgress<string> progress)
        {
            var hashD = AppPanels.GetImgPanel(idpanel).Hash;
            Delete(hashD, progress);
            Confirm(1 - idpanel, false);
            var hashV = AppPanels.GetImgPanel(1 - idpanel).Hash;
            var lc = DateTime.Now.AddYears(-5);
            AppDatabase.ImgUpdateLastCheck(hashV, lc);
        }
    }
} 