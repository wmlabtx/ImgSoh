using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Find(string hashX, IProgress<string> progress)
        {
            Img imgX = null;
            do {
                var totalcount = AppImgs.Count();
                if (totalcount < 2) {
                    progress?.Report($"totalcount = {totalcount}");
                    return;
                }

                if (hashX == null) {
                    imgX = AppImgs.GetNextView();
                    if (imgX == null) {
                        return;
                    }

                    hashX = imgX.Hash;
                }

                if (!AppPanels.SetImgPanel(0, hashX, AppConsts.PathGbProtected)) {
                    Delete(imgX, progress);
                    progress?.Report($"{hashX} deleted");
                    hashX = null;
                    continue;
                }

                imgX = AppPanels.GetImgPanel(0).Img;
                var hashY = imgX.Next;
                if (!AppPanels.SetImgPanel(1, hashY, AppConsts.PathGbProtected)) {
                    hashX = null;
                    continue;
                }

                AppPanels.UpdateStatus(progress);

                break;
            }
            while (true);
        }
    }
}
