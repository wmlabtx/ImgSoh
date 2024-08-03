namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;

            if (AppImgs.TryGetImg(hashY, out var imgY)) {
                AppImgs.UpdateRank(imgY);
                if (string.IsNullOrEmpty(imgY.Family)) {
                    var family = AppImgs.GetNewFamily();
                    AppDatabase.SetFamily(imgY.Hash, family);
                }
            }

            if (AppImgs.TryGetImg(hashX, out var imgX)) {
                AppImgs.UpdateRank(imgX);
                if (string.IsNullOrEmpty(imgX.Family)) {
                    var family = AppImgs.GetNewFamily();
                    AppDatabase.SetFamily(imgX.Hash, family);
                }

                if (hashY.Equals(imgX.Next.Substring(4))) {
                    AppDatabase.SetHorizon(hashX);
                    AppDatabase.SetCounter(hashX, imgX.Counter + 1);

                    if (imgY.Next.Equals(imgX.Hash)) {
                        AppDatabase.SetHorizon(hashY);
                        AppDatabase.SetCounter(hashY, imgY.Counter + 1);
                    }
                }
            }

            AppDatabase.SetVerified(hashX);
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            if (AppImgs.TryGetImg(hashX, out var imgX)) {
                AppImgs.UpdateRank(imgX);
            }
        }
    }
} 