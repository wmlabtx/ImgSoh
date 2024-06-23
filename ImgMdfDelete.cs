using System;
using System.IO;
using ImageMagick;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        private static void Delete(string hashD)
        {
            if (!AppImgs.TryGetImg(hashD, out var imgX)) {
                return;
            }

            var filename = AppFile.GetFileName(imgX.Name, AppConsts.PathHp);
            DeleteEncryptedFile(filename);
            AppDatabase.ImgDelete(hashD);
            AppImgs.Remove(hashD);
        }

        private static void DeleteFile(string filename)
        {
            if (!File.Exists(filename)) {
                return;
            }

            File.SetAttributes(filename, FileAttributes.Normal);
            var name = Path.GetFileNameWithoutExtension(filename).ToLower();
            var ext = Path.GetExtension(filename);
            if (ext.StartsWith(".")) {
                ext = ext.Substring(1);
            }

            var recycledName = AppFile.GetRecycledName(name, ext, AppConsts.PathGbProtected, DateTime.Now);
            AppFile.CreateDirectory(recycledName);
            File.Move(filename, recycledName);
        }

        private static void DeleteEncryptedFile(string filename)
        {
            if (!File.Exists(filename)) {
                return;
            }

            File.SetAttributes(filename, FileAttributes.Normal);
            var name = Path.GetFileNameWithoutExtension(filename).ToLower();
            var array = AppFile.ReadEncryptedFile(filename);
            string ext;
            using (var magicImage = new MagickImage()) {
                magicImage.Ping(array);
                ext = magicImage.Format.ToString().ToLower();
            }

            var recycledName = AppFile.GetRecycledName(name, ext, AppConsts.PathGbProtected, DateTime.Now);
            AppFile.CreateDirectory(recycledName);
            File.WriteAllBytes(recycledName, array);
            File.Delete(filename);
        }

        public static void Delete(int idpanel)
        {
            var hashD = AppPanels.GetImgPanel(idpanel).Hash;
            Delete(hashD);
            ConfirmOpposite(1 - idpanel);
        }
    }
} 