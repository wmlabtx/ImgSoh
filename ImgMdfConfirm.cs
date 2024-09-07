namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;
            if (AppImgs.TryGetImg(hashY, out var imgY)) {
                AppDatabase.UpdateViewed(hashY, imgY.Viewed);
            }

            if (AppImgs.TryGetImg(hashX, out var imgX)) {
                var lastcheck = AppDatabase.GetMinimal(AppConsts.AttributeLastCheck);
                AppDatabase.UpdateViewed(hashX, imgX.Viewed);
                if (hashY.Equals(imgX.Next.Substring(4))) {
                    AppDatabase.SetHorizon(hashX, imgX.Next);
                    AppDatabase.SetCounter(hashX, imgX.Counter + 1);
                    AppDatabase.SetLastCheck(hashX, lastcheck);
                }

                if (imgY.Next.Length > 4 && hashX.Equals(imgY.Next.Substring(4))) {
                    AppDatabase.SetHorizon(hashY, imgY.Next);
                    AppDatabase.SetCounter(hashY, imgY.Counter + 1);
                    AppDatabase.SetLastCheck(hashY, lastcheck);
                }
            }

            AppDatabase.SetVerified(hashX);
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            if (AppImgs.TryGetImg(hashX, out var imgX)) {
                var lastcheck = AppDatabase.GetMinimal(AppConsts.AttributeLastCheck);
                AppDatabase.UpdateViewed(hashX, imgX.Viewed);
                AppDatabase.SetLastCheck(hashX, lastcheck);
            }
        }
    }
} 