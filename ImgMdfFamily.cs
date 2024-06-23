namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void CombineToFamily()
        {
            if (!AppImgs.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                return;
            }

            if (!AppImgs.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                return;
            }

            if (imgX.Family == 0 && imgY.Family == 0) {
                var family = AppImgs.GetNewFamily();
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
                                AppImgs.RenameFamily(imgY.Family, imgX.Family);
                            }
                            else {
                                AppImgs.RenameFamily(imgX.Family, imgY.Family);
                            }
                        }
                    }
                }
            }
        }

        public static void DetachFromFamily()
        {
            if (AppImgs.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                var family = AppImgs.GetFamily(imgX.Family);
                foreach (var e in family) {
                    AppDatabase.SetFamily(e.Hash, 0);
                }
            }
        }
    }
}
