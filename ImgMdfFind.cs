using System;
using System.Linq;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Find(string hashX, IProgress<string> progress)
        {
            do {
                var totalcount = AppDatabase.ImgCount(false);
                if (totalcount < 2) {
                    progress?.Report($"totalcount = {totalcount}");
                    return;
                }

                if (hashX == null) {
                    hashX = AppDatabase.GetNextView();
                    if (hashX == null) {
                        progress?.Report($"not ready to view");
                        return;
                    }
                }

                if (!AppPanels.SetImgPanel(0, hashX)) {
                    Delete(hashX, progress);
                    hashX = null;
                    continue;
                }

                if (!AppDatabase.TryGetImg(hashX, out var imgX)) {
                    Delete(hashX, progress);
                    hashX = null;
                    continue;
                }

                /*
                 * 0) Next
                 * 1) oldest from History if History > 0
                 * 2) oldest from Family if Family > 0
                 */

                Img imgY = null;
                if (imgX.Family > 0) {
                    var coin = AppVars.RandomNext(10);
                    if (coin == 0) {
                        var family = AppDatabase.GetFamily(imgX.Family).ToList();
                        family.Remove(imgX.Hash);
                        if (family.Count > 0) {
                            foreach (var hash in family) {
                                if (!hash.Equals(imgX.Hash) && AppDatabase.TryGetImg(hash, out var img)) {
                                    if (imgY == null || img.LastView < imgY.LastView) {
                                        imgY = img;
                                    }
                                }
                            }
                        }
                    }
                }

                if (imgY == null && imgX.HistoryCount > 0) {
                    var coin = AppVars.RandomNext(10);
                    if (coin == 0) {
                        var historyArray = imgX.HistoryArray;
                        foreach (var hash in historyArray) {
                            if (!hash.Equals(imgX.Hash) && AppDatabase.TryGetImg(hash, out var img)) {
                                if (imgY == null || img.LastView < imgY.LastView) {
                                    imgY = img;
                                }
                            }
                        }
                    }
                }

                var hashY = imgY == null ? imgX.Next : imgY.Hash;
                if (!string.IsNullOrEmpty(hashY)) {
                    var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                    var shortfilename = Helper.GetShortFileName(imgX.Folder, hashX);
                    var imgcount = AppDatabase.ImgCount(false);
                    var newimgcount = AppDatabase.ImgCount(true);
                    progress.Report($"{newimgcount}/{imgcount}: [{age} ago] {shortfilename}");

                    if (hashX.Equals(hashY)) {
                        throw new Exception();
                    }

                    if (!AppPanels.SetImgPanel(1, hashY)) {
                        Delete(hashY, progress);
                        hashX = null;
                        continue;
                    }

                    break;
                }

                hashX = null;
            }
            while (true);
        }
    }
}
