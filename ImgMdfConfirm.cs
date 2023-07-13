using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var imgX = AppPanels.GetImgPanel(0).Img;
            AppImgs.SetLastView(imgX.Hash, DateTime.Now);
            var lc = AppImgs.GetMinLastCheck();
            AppImgs.SetLastCheck(imgX.Hash, lc);
            var imgY = AppPanels.GetImgPanel(1).Img;
            AppImgs.SetLastCheck(imgY.Hash, lc);
        }
    }
} 