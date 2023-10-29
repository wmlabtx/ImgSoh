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
                    imgX.AddToHistory(hashY);
                    imgX.SetLastView(DateTime.Now);
                    imgX.SetVerified(true);
                    imgX.SetNext(string.Empty);
                    if (imgX.Family == 0) {
                        var family = AppDatabase.SuggestFamilyId();
                        imgX.SetFamily(family);
                    }

                    imgY.AddToHistory(hashX);
                    imgY.SetLastView(DateTime.Now.AddMinutes(-1));
                    imgY.SetNext(string.Empty);
                    if (imgY.Family == 0) {
                        var family = AppDatabase.SuggestFamilyId();
                        imgY.SetFamily(family);
                    }

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

                var lc = AppDatabase.GetMinLastCheck();
                imgX.SetLastCheck(lc);
            }
        }
    }
} 