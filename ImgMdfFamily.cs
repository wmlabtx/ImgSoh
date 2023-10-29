namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void CombineToFamily()
        {
            if (AppDatabase.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                if (AppDatabase.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                    if (imgX.Family == 0 && imgY.Family == 0) {
                        var family = AppDatabase.SuggestFamilyId();
                        imgX.SetFamily(family);
                        imgY.SetFamily(family);
                    }
                    else {
                        if (imgX.Family == 0 && imgY.Family != 0) {
                            imgX.SetFamily(imgY.Family);
                        }
                        else {
                            if (imgX.Family != 0 && imgY.Family == 0) {
                                imgY.SetFamily(imgX.Family);
                            }
                            else {
                                if (imgX.Family != 0 && imgY.Family != 0) {
                                    if (imgX.Family < imgY.Family) {
                                        AppDatabase.RenameFamily(imgY.Family, imgX.Family);
                                    }
                                    else {
                                        if (imgX.Family > imgY.Family) {
                                            AppDatabase.RenameFamily(imgX.Family, imgY.Family);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void DetachFromFamily()
        {
            if (AppDatabase.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                if (AppDatabase.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                    imgX.SetFamily(0);
                    imgY.SetFamily(0);
                }
            }
        }
    }
}
