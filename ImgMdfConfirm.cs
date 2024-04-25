namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;

            if (AppDatabase.TryGetImg(hashX, out var imgX) && AppDatabase.TryGetImg(hashY, out var imgY)) {
                if (imgX.Family != imgY.Family || (imgX.Family <= 0 && imgY.Family <= 0)) {
                    AppDatabase.AddToHistory(hashY, hashX);
                    AppDatabase.AddToHistory(hashX, hashY);
                }
            }

            AppDatabase.SetVerified(hashY);
            AppDatabase.SetLastView(hashY);
            AppDatabase.SetNext(hashY, hashY);
            AppDatabase.SetLastCheck(hashY, AppDatabase.GetMinLastCheck());

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