using System;
using System.Linq;

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
                if (imgX.Family > 0 && AppVars.RandomNext(5) == 0) {
                    var members = AppDatabase.GetFamily(imgX.Family);
                    var list = members.ToList();
                    list.Remove(hashX);
                    if (list.Count > 0) {
                        var rindex = AppVars.RandomNext(list.Count);
                        hashY = list[rindex];
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
