namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;
            
            AppDatabase.SetFamilyForced(hashY);
            AppDatabase.AddToHistory(hashY, hashX);
            AppDatabase.SetLastView(hashY);
            AppDatabase.SetLastCheck(hashY, AppDatabase.GetMinLastCheck());

            AppDatabase.SetFamilyForced(hashX);
            AppDatabase.SetVerified(hashX);
            AppDatabase.AddToHistory(hashX, hashY);
            AppDatabase.SetLastView(hashX);
            AppDatabase.SetLastCheck(hashX, AppDatabase.GetMinLastCheck());
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;

            AppDatabase.SetFamilyForced(hashX);
            AppDatabase.SetLastView(hashX);
        }
    }
} 