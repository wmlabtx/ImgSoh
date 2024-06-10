using System;
using System.IO;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        private static void Delete(string hashD)
        {
            if (!AppDatabase.TryGetImg(hashD, out var imgD)) {
                return;
            }

            AppDatabase.ImgDelete(hashD);
            var pathD = imgD.Path;
            var extD = imgD.Ext;
            var filename = Helper.GetFileName(pathD, hashD, extD);
            DeleteFile(filename);
        }

        private static void DeleteFile(string filename)
        {
            if (!File.Exists(filename)) {
                return;
            }

            var name = Path.GetFileNameWithoutExtension(filename).ToLower();
            var extension = Path.GetExtension(filename);
            if (extension.StartsWith(".")) {
                extension = extension.Substring(1);
            }

            var now = DateTime.Now;
            File.SetAttributes(filename, FileAttributes.Normal);
            string deletedFilename;
            var counter = 0;
            do {
                deletedFilename = $"{AppConsts.PathGb}\\{now.Year}-{now.Month:D2}-{now.Day:D2}\\{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.{name}";
                if (counter > 0) {
                    deletedFilename += $"({counter})";
                }

                deletedFilename += $".{extension}";
                counter++;
            }
            while (File.Exists(deletedFilename));
            FileHelper.CreateDirectory(deletedFilename);
            File.Move(filename, deletedFilename);
        }

        public static void Delete(int idpanel)
        {
            var hashD = AppPanels.GetImgPanel(idpanel).Hash;
            Delete(hashD);
            ConfirmOpposite(1 - idpanel);
        }
    }
} 