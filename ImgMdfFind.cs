using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Find(string hashX, IProgress<string> progress)
        {
            var status = string.Empty;
            do {
                var totalcount = AppDatabase.ImgCount(false);
                if (totalcount < 2) {
                    progress?.Report($"totalcount = {totalcount}");
                    return;
                }

                if (hashX == null) {
                    AppDatabase.GetNextView(out var hash, out status);
                    hashX = hash;
                    if (hashX == null) {
                        progress?.Report("not ready to view");
                        return;
                    }
                }

                if (!AppPanels.SetImgPanel(0, hashX)) {
                    Delete(hashX, AppConsts.CorruptedExtension, progress);
                    hashX = null;
                    continue;
                }

                if (!AppDatabase.TryGetImg(hashX, out var imgX)) {
                    Delete(hashX, AppConsts.CorruptedExtension, progress);
                    hashX = null;
                    continue;
                }

                Img imgY = null;

                if (imgX.HistoryCount >= AppConsts.MaxHistorySize) {
                    var historyArray = imgX.HistoryArray;
                    foreach (var hash in historyArray) {
                        if (!hash.Equals(imgX.Hash) && AppDatabase.TryGetImg(hash, out var img)) {
                            if (imgY == null || img.LastView < imgY.LastView) {
                                imgY = img;
                            }
                        }
                    }
                }

                var hashY = imgY == null ? imgX.Next : imgY.Hash;
                if (!string.IsNullOrEmpty(hashY)) {
                    var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                    var shortfilename = Helper.GetShortFileName(imgX.Folder, hashX);
                    progress.Report($"{status} [{age} ago] {shortfilename}");

                    if (hashX.Equals(hashY)) {
                        throw new Exception();
                    }

                    if (!AppPanels.SetImgPanel(1, hashY)) {
                        Delete(hashY, AppConsts.CorruptedExtension, progress);
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
