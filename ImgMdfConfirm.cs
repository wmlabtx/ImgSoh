namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            AppDatabase.SetVerified(hashX);
            AppDatabase.SetLastView(hashX);
            
            var hashY = AppPanels.GetImgPanel(1).Hash;
            AppDatabase.SetLastView(hashY);

            AppDatabase.AddToHistory(hashX, hashY);
            AppDatabase.AddToHistory(hashY, hashX);
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            AppDatabase.SetLastView(hashX);
        }
    }
} 