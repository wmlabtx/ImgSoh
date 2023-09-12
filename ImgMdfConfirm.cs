namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm(int idpanel, bool setVerified)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            AppDatabase.Confirm(hashX, setVerified);

            var hashY = AppPanels.GetImgPanel(1 - idpanel).Hash;
            if (AppDatabase.TryGetImg(hashX, out var imgX)) {
                if (AppDatabase.TryGetImg(hashY, out var imgY)) {
                    if (!imgX.IsInFamily(hashY) && !imgX.IsInAliens(hashY)) {
                        imgX.AddToAliens(hashY);
                        imgY.RemoveFromFamily(hashX);
                        imgY.AddToAliens(hashX);
                    }
                }
            }
        }
    }
} 