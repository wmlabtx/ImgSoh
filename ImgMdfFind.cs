using System;

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

                var hashY = imgX.Next;
                if (!string.IsNullOrEmpty(hashY)) {
                    var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                    var shortfilename = Helper.GetShortFileName(imgX.Folder, hashX);
                    var imgcount = AppDatabase.ImgCount(false);
                    var newimgcount = AppDatabase.ImgCount(true);
                    var paircount = AppDatabase.PairCount();
                    progress.Report($"{newimgcount}/p{paircount}/{imgcount}: [{age} ago] {shortfilename}");

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
