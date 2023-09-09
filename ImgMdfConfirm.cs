using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Confirm(int idpanel, bool setVerified)
        {
            var hash = AppPanels.GetImgPanel(idpanel).Hash;
            if (setVerified) {
                AppDatabase.ImgUpdateVerified(hash);
                var hashX = AppPanels.GetImgPanel(0).Hash;
                var hashY = AppPanels.GetImgPanel(1).Hash;
                if (AppDatabase.AddPair(hashX, hashY)) {
                    var lc = DateTime.Now.AddYears(-5);
                    AppDatabase.ImgUpdateLastCheck(hashX, lc);
                    AppDatabase.ImgUpdateLastCheck(hashY, lc);
                }
            }

            AppDatabase.Confirm(hash);
        }
    }
} 