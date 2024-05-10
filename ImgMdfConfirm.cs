﻿namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm()
        {
            var hashX = AppPanels.GetImgPanel(0).Hash;
            var hashY = AppPanels.GetImgPanel(1).Hash;

            if (AppDatabase.TryGetImg(hashY, out var imgY)) {
                if (imgY.Family == 0) {
                    var family = AppDatabase.GetNewFamily();
                    AppDatabase.SetFamily(hashY, family);
                }
            }

            AppDatabase.SetLastView(hashY);
            AppDatabase.SetNext(hashY, hashY);
            AppDatabase.SetLastCheck(hashY, AppDatabase.GetMinLastCheck());

            if (AppDatabase.TryGetImg(hashX, out var imgX)) {
                if (imgX.Family == 0) {
                    var family = AppDatabase.GetNewFamily();
                    AppDatabase.SetFamily(hashX, family);
                }
            }

            AppDatabase.SetVerified(hashX);
            AppDatabase.SetLastView(hashX);
            AppDatabase.AddToHistory(hashX, hashY);
            AppDatabase.SetNext(hashX, hashX);
            AppDatabase.SetLastCheck(hashX, AppDatabase.GetMinLastCheck());
        }

        private static void ConfirmOpposite(int idpanel)
        {
            var hashX = AppPanels.GetImgPanel(idpanel).Hash;
            AppDatabase.SetNext(hashX, hashX);
            AppDatabase.SetLastView(hashX);
            AppDatabase.SetLastCheck(hashX, AppDatabase.GetMinLastCheck());
        }
    }
} 