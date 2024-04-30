namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;

            if (AppDatabase.TryGetImg(hashX, out var imgX)) {
                if (imgX.Next.Equals(hashY)) {
                    AppDatabase.SetHorizon(hashX, imgX.Horizon + 1);
                }
            }

            AppDatabase.SetLastView(hashY);
            AppDatabase.SetNext(hashY, hashY);

            AppDatabase.SetVerified(hashX);
            AppDatabase.SetLastView(hashX);
            AppDatabase.SetNext(hashX, hashX);
            AppDatabase.SetLastCheck(hashX, AppDatabase.GetMinLastCheck());
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            AppDatabase.SetLastCheck(hashX, AppDatabase.GetMinLastCheck());
            AppDatabase.SetLastView(hashX);
        }
    }
} 