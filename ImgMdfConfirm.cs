using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            if (AppDatabase.TryGetImg(hashX, out var imgX)) {
                var hashY = AppPanels.GetImgPanel(1).Hash;
                if (AppDatabase.TryGetImg(hashY, out var imgY)) {
                    if (imgX.Family == 0) {
                        var family = AppDatabase.GetNewFamily();
                        imgX.SetFamily(family);
                    }

                    if (imgX.Family != imgY.Family) {
                        imgX.AddToHistory(hashY);
                        imgY.AddToHistory(hashX);
                    }

                    imgX.SetLastView(DateTime.Now);
                    imgX.SetVerified(true);

                    if (imgY.Family == 0) {
                        var family = AppDatabase.GetNewFamily();
                        imgY.SetFamily(family);
                    }

                    imgY.SetLastView(DateTime.Now);
                }
            }
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            if (AppDatabase.TryGetImg(hashX, out var imgX)) {
                if (imgX.Verified) {
                    imgX.SetLastView(DateTime.Now);
                }
            }
        }
    }
} 