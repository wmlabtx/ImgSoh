using System;

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
                imgX.SetFamily(family);
                imgY.SetFamily(family);
            }
            else {
                if (imgX.Family > 0 && imgY.Family == 0) {
                    imgY.SetFamily(imgX.Family);
                }
                else {
                    if (imgX.Family == 0 && imgY.Family > 0) {
                        imgX.SetFamily(imgY.Family);
                    }
                    else {
                        if (imgX.Family != imgY.Family) {
                            var family = Math.Min(imgX.Family, imgY.Family);
                            imgX.SetFamily(family);
                            imgY.SetFamily(family);
                        }
                    }
                }
            }
        }

        public static void DetachFromFamily()
        {
            if (!AppDatabase.TryGetImg(AppPanels.GetImgPanel(0).Hash, out var imgX)) {
                return;
            }

            var family = AppDatabase.GetNewFamily();
            imgX.SetFamily(family);
        }
    }
}
