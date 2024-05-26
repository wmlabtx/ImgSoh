namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;

            AppDatabase.SetLastView(hashY);

            if (AppDatabase.TryGetImg(hashX, out var imgX) && AppDatabase.TryGetImg(hashY, out var imgY)) {
                if (hashY.Equals(imgX.Next.Substring(4))) {
                    AppDatabase.SetHorizon(hashX);
                }

                if (!string.IsNullOrWhiteSpace(imgY.Next) && hashX.Equals(imgY.Next.Substring(4))) {
                    AppDatabase.SetHorizon(hashY);
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