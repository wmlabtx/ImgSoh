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

                //var panels = new ImgPanel[2];
                //panels[0] = AppPanels.GetImgPanel(0);
                imgX = AppPanels.GetImgPanel(0).Img;
                var similars = GetSimilars(imgX, progress);
                AppPanels.SetSimilars(similars, progress, AppConsts.PathGbProtected);
                /*
                panels[1] = AppPanels.GetImgPanel(1);
                if (panels[0].Img.Distance < 0.01f && panels[0].Bitmap.Width == panels[1].Bitmap.Width && panels[0].Bitmap.Height == panels[1].Bitmap.Height) {
                    if ((panels[0].DateTaken.Year == 2020 && (panels[0].DateTaken.Month == 3 || panels[0].DateTaken.Month == 4) && panels[1].DateTaken.Year < 2020) ||
                        (panels[0].DateTaken.Year == 2022 && panels[0].DateTaken.Month == 11 && panels[0].DateTaken.Day == 28 && panels[1].DateTaken.Year < 2020)) {
                        Delete(hashX, progress);
                        hashX = null;
                        continue;
                    }
                    else {
                        if ((panels[1].DateTaken.Year == 2020 && (panels[1].DateTaken.Month == 3 || panels[1].DateTaken.Month == 4) && panels[0].DateTaken.Year < 2020) ||
                            (panels[1].DateTaken.Year == 2022 && panels[1].DateTaken.Month == 11 && panels[1].DateTaken.Day == 28 && panels[0].DateTaken.Year < 2020)) {
                            var hashY = panels[1].Img.Hash;
                            Delete(hashY, progress);
                            hashX = null;
                            continue;
                        }
                    }
                }
                */

                break;
            }
            while (true);
        }
    }
}
