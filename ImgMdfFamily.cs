using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        /*
        private static void RenameFamily(int ofamily, int nfamily, IProgress<string> progress)
        {
            var fo = AppDatabase.GetFamily(ofamily);
            var fn = AppDatabase.GetFamily(nfamily);
            foreach (var imgo in fo) {
                foreach (var imgn in fn) {
                    if (imgo.Next.Equals(imgn.Hash)) {
                        AppDatabase.SetNext(imgo.Hash, imgo.Hash);
                    }

                    if (imgn.Next.Equals(imgo.Hash)) {
                        AppDatabase.SetNext(imgn.Hash, imgn.Hash);
                    }

                    progress.Report($"{imgo.Hash} = {nfamily}");
                    AppDatabase.SetFamily(imgo.Hash, nfamily);
                }
            }
        }

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
                if (imgX.Next.Equals(imgY.Hash)) {
                    AppDatabase.SetNext(imgX.Hash, imgX.Hash);
                }

                if (imgY.Next.Equals(imgX.Hash)) {
                    AppDatabase.SetNext(imgY.Hash, imgY.Hash);
                }

                AppDatabase.SetFamily(imgX.Hash, family);
                AppDatabase.SetFamily(imgY.Hash, family);
            }
            else {
                if (imgX.Family > 0 && imgY.Family == 0) {
                    var fs = AppDatabase.GetFamily(imgX.Family);
                    foreach (var img in fs) {
                        if (imgX.Next.Equals(img.Hash)) {
                            AppDatabase.SetNext(imgX.Hash, imgX.Hash);
                        }

                        if (img.Next.Equals(imgX.Hash)) {
                            AppDatabase.SetNext(img.Hash, img.Hash);
                        }
                    }

                    AppDatabase.SetFamily(imgY.Hash, imgX.Family);
                }
                else {
                    if (imgX.Family == 0 && imgY.Family > 0) {
                        var fs = AppDatabase.GetFamily(imgY.Family);
                        foreach (var img in fs) {
                            if (imgX.Next.Equals(img.Hash)) {
                                AppDatabase.SetNext(imgX.Hash, imgX.Hash);
                            }

                            if (img.Next.Equals(imgX.Hash)) {
                                AppDatabase.SetNext(img.Hash, img.Hash);
                            }
                        }

                        AppDatabase.SetFamily(imgX.Hash, imgY.Family);
                    }
                    else {
                        if (imgX.Family != imgY.Family) {
                            if (imgX.Family < imgY.Family) {
                                RenameFamily(imgY.Family, imgX.Family, progress);
                            }
                            else {
                                RenameFamily(imgX.Family, imgY.Family, progress);
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
        */
    }
}
