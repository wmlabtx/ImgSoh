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

                var hashY = imgX.Next;
                var historyarray = imgX.GetHistoryArray();
                if (historyarray.Length > 0 && AppVars.RandomNext(10) == 0) {
                    var lv = DateTime.MaxValue;
                    foreach (var hash in historyarray) {
                        if (AppDatabase.TryGetImg(hash, out var img)) {
                            if (img.LastView < lv) {
                                hashY = hash;
                                lv = img.LastView;
                            }
                        }
                    }
                }

                if (hashX.Equals(hashY)) {
                    throw new Exception();
                }

                if (!AppPanels.SetImgPanel(1, hashY)) {
                    Delete(hashY, AppConsts.CorruptedExtension, progress);
                    hashX = null;
                    continue;
                }

                if (!AppDatabase.TryGetImg(hashY, out var imgY)) {
                    Delete(hashY, AppConsts.CorruptedExtension, progress);
                    hashX = null;
                    continue;
                }

                var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                var shortfilename = Helper.GetShortFileName(imgX.Folder, hashX);
                var distance = VitHelper.GetDistance(imgX.GetVector(), imgY.GetVector());
                progress.Report($"{status} [{age} ago] {shortfilename} {distance:F4}");
                break;
            }
            while (true);
        }
    }
}
