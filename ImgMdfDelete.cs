using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        private static void Delete(Img imgD, IProgress<string> progress)
        {
            progress.Report($"Delete {imgD.GetShortFileName()}");
            AppImgs.Delete(imgD);
            var filename = imgD.GetFileName();
            FileHelper.DeleteToRecycleBin(filename, AppConsts.PathDeProtected);
            AppDatabase.DeleteImage(imgD.Hash);
            AppDatabase.DeletePair(imgD.Hash);
        }

        public static void Delete(int idpanel, IProgress<string> progress)
        {
            var imgD = AppPanels.GetImgPanel(idpanel).Img;
            Delete(imgD, progress);
            Confirm(1 - idpanel);
        }
    }
} 