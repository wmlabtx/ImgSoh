namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void CombineToFamily()
        {
            /*
            if (!AppDatabase.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                return;
            }

            if (!AppDatabase.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                return;
            }

            if (imgX.Family == 0 && imgY.Family == 0) {
                var family = AppDatabase.GetNewFamily();
                AppDatabase.SetFamily(imgX.Hash, family);
                AppDatabase.SetFamily(imgY.Hash, family);
            }
            else {
                if (imgX.Family > 0 && imgY.Family == 0) {
                    AppDatabase.SetFamily(imgY.Hash, imgX.Family);
                }
                else {
                    if (imgX.Family == 0 && imgY.Family > 0) {
                        AppDatabase.SetFamily(imgX.Hash, imgY.Family);
                    }
                    else {
                        if (imgX.Family != imgY.Family) {
                            var family = Math.Min(imgX.Family, imgY.Family);
                            AppDatabase.SetFamily(imgX.Hash, family);
                            AppDatabase.SetFamily(imgY.Hash, family);
                        }
                    }
                }
            }
            */
        }

        public static void DetachFromFamily()
        {
            /*
            if (AppDatabase.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                var family = AppDatabase.GetNewFamily();
                AppDatabase.SetFamily(imgX.Hash, family);
            }

            if (AppDatabase.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                var family = AppDatabase.GetNewFamily();
                AppDatabase.SetFamily(imgY.Hash, family);
            }
            */
        }
    }
}
