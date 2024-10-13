namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;
            if (AppImgs.TryGet(hashY, out var imgY)) {
                AppImgs.UpdateViewed(hashY, imgY.Viewed);
            }

            if (AppImgs.TryGet(hashX, out var imgX)) {
                AppImgs.UpdateViewed(hashX, imgX.Viewed);
                if (hashY.Equals(imgX.Next.Substring(4))) {
                    AppImgs.SetHorizon(hashX, imgX.Next);
                    AppImgs.SetCounter(hashX, imgX.Counter + 1);
                }

                if (imgY.Next.Length > 4 && hashX.Equals(imgY.Next.Substring(4))) {
                    AppImgs.SetHorizon(hashY, imgY.Next);
                    AppImgs.SetCounter(hashY, imgY.Counter + 1);
                }
            }

            AppImgs.SetVerified(hashX);
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            if (AppImgs.TryGet(hashX, out var imgX)) {
                AppImgs.UpdateViewed(hashX, imgX.Viewed);
            }
        }
    }
} 