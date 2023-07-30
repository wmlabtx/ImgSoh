using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm(int idpanel)
        {
            var imgX = AppPanels.GetImgPanel(idpanel).Img;
            AppImgs.SetLastView(imgX.Hash, DateTime.Now);
            var review = (short)(imgX.Review + 1);
            AppImgs.SetReview(imgX.Hash, review);
            var lc = AppImgs.GetMinLastCheck();
            AppImgs.SetLastCheck(imgX.Hash, lc);
        }
    }
} 