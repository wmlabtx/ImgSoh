namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;

            AppDatabase.SetLastView(hashY);

            if (AppImgs.TryGetImg(hashX, out var imgX)) {
                if (hashY.Equals(imgX.Next.Substring(4))) {
                    AppDatabase.SetHorizon(hashX);
                    AppDatabase.SetCounter(hashX, imgX.Counter + 1);
                }
            }

            AppDatabase.SetVerified(hashX);
            AppDatabase.SetLastView(hashX);
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            AppDatabase.SetLastView(hashX);
        }
    }
} 