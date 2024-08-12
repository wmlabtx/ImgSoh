namespace ImgSoh
{
    public static partial class ImgMdf
    {
        /*
        public static void CombineToFamily()
        {
            if (!AppImgs.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                return;
            }

            if (!AppImgs.TryGetImg(AppPanels.GetImgPanel(1).Hash, out var imgY)) {
                return;
            }

            if (string.IsNullOrEmpty(imgX.Family) && string.IsNullOrEmpty(imgY.Family)) {
                var family = AppImgs.GetNewFamily();
                AppDatabase.SetFamily(imgX.Hash, family);
                AppDatabase.SetFamily(imgY.Hash, family);
            }
            else {
                if (string.IsNullOrEmpty(imgY.Family)) {
                    AppDatabase.SetFamily(imgY.Hash, imgX.Family);
                }
                else {
                    if (string.IsNullOrEmpty(imgX.Family)) {
                        AppDatabase.SetFamily(imgX.Hash, imgY.Family);
                    }
                    else {
                        if (!imgX.Family.Equals(imgY.Family)) {
                            AppImgs.RenameFamily(imgY.Family, imgX.Family);
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
                    AppDatabase.SetFamily(e.Hash, string.Empty);
                }
            }
        }
        */
    }
}
