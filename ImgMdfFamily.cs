using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void CombineToFamily(IProgress<string> progress)
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
                    AppDatabase.InvalidateFamily(imgX.Family, progress);
                }
                else {
                    if (imgX.Family == 0 && imgY.Family > 0) {
                        AppDatabase.SetFamily(imgX.Hash, imgY.Family);
                        AppDatabase.InvalidateFamily(imgY.Family, progress);
                    }
                    else {
                        if (imgX.Family != imgY.Family) {
                            if (imgX.Family < imgY.Family) {
                                AppDatabase.RenameFamily(imgY.Family, imgX.Family, progress);
                                AppDatabase.InvalidateFamily(imgX.Family, progress);
                            }
                            else {
                                AppDatabase.RenameFamily(imgX.Family, imgY.Family, progress);
                                AppDatabase.InvalidateFamily(imgY.Family, progress);
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
    }
}
