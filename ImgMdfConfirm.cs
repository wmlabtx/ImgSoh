using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var imgX = AppPanels.GetImgPanel(0).Img;
            AppImgs.SetLastView(imgX.Hash, DateTime.Now);
            
            var imgY = AppPanels.GetImgPanel(1).Img;
            imgY.SetLastView(DateTime.Now);
            AppImgs.SetLastView(imgY.Hash, DateTime.Now);

            if (imgX.Review < imgY.Review) {
                imgX.IncrementReview();
                AppImgs.IncrementReview(imgX.Hash);
            }
            else {
                if (imgX.Review == imgY.Review) {
                    AppImgs.IncrementReview(imgX.Hash);
                    AppImgs.IncrementReview(imgY.Hash);
                }
                else {
                    AppImgs.IncrementReview(imgY.Hash);
                }
            }
        }
    }
} 