namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void CombineToFamily()
        {
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
                            if (imgX.Family < imgY.Family) {
                                AppDatabase.RenameFamily(imgY.Family, imgX.Family);
                            }
                            else {
                                AppDatabase.RenameFamily(imgX.Family, imgY.Family);
                            }
                        }
                    }
                }
            }
        }

        public static void DetachFromFamily()
        {
            if (AppDatabase.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                AppDatabase.SetFamily(imgX.Hash, 0);
            }

            if (AppDatabase.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                AppDatabase.SetFamily(imgY.Hash, 0);
            }
        }

        /*
        public static void RefreshFamily(int family)
        {
            if (family <= 0) {
                return;
            }

            var familyarray = AppDatabase.GetFamily(family);
            Img oldest = null;
            foreach (var img in familyarray) {
                if (oldest == null || img.LastView < oldest.LastView) {
                    oldest = img;
                }
            }

            if (oldest != null) {
                AppDatabase.SetNext(oldest.Hash, oldest.Hash);
            }
        }
        */
    }
}
