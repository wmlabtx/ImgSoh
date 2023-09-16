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

                // 0 - family
                // 1 - history
                // 2 - any others

                var lvs = AppDatabase.GetLastViews();
                var minlvs = new[] { DateTime.MaxValue, DateTime.MaxValue, DateTime.MaxValue };
                var hashes = new[] { null, null, imgX.Next };
                if (imgX.Family > 0) {
                    var family = AppDatabase.GetFamily(imgX.Family);
                    foreach (var hash in family) {
                        if (hash.Equals(imgX.Hash)) {
                            continue;
                        }

                        if (lvs.TryGetValue(hash, out var lv)) {
                            if (lv < minlvs[0]) {
                                minlvs[0] = lv;
                                hashes[0] = hash;
                            }
                        }
                    }
                }

                if (imgX.HistoryCount > 0) {
                    var historyArray = imgX.HistoryArray;
                    foreach (var hash in historyArray) {
                        if (lvs.TryGetValue(hash, out var lv)) {
                            if (lv < minlvs[1]) {
                                minlvs[1] = lv;
                                hashes[1] = hash;
                            }
                        }
                    }
                }

                int mode;
                do {
                    mode = AppVars.RandomNext(3);
                } while (hashes[mode] == null);
                var hashY = hashes[mode];

                if (!string.IsNullOrEmpty(hashY)) {
                    var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                    var shortfilename = Helper.GetShortFileName(imgX.Folder, hashX);
                    var imgcount = AppDatabase.ImgCount(false);
                    var newimgcount = AppDatabase.ImgCount(true);
                    progress.Report($"{newimgcount}/{imgcount}: [{age} ago] {shortfilename}");

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

/*



            var minlvs = new[] { DateTime.MaxValue, DateTime.MaxValue, DateTime.MaxValue };
                var hashes = new string[] { null, null, null };
                var mindistances = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
                
                foreach (var e in vectors) {
                    if (imgX.IsInFamily(e.Key)) {
                        if (e.Value.Item2 < minlvs[0]) {
                            minlvs[0] = e.Value.Item2;
                            hashes[0] = e.Key;
                        }
                    }
                    else {
                        if (imgX.IsInAliens(e.Key)) {
                            if (e.Value.Item2 < minlvs[1]) {
                                minlvs[1] = e.Value.Item2;
                                hashes[1] = e.Key;
                            }
                        }
                        else {
                            var distance = VggHelper.GetDistance(vectorX, e.Value.Item1);
                            if (distance < mindistances[2]) {
                                mindistances[2] = distance;
                                hashes[2] = e.Key;
                            }
                        }
                    }
                }

                if (hashes[0] != null || hashes[1] != null || hashes[2] != null) {
                    int mode;
                    do {
                        mode = AppVars.RandomNext(3);
                    } while (hashes[mode] == null);

                    if (!imgX.Next.Equals(hashes[mode])) {
                        var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                        var shortfilename = Helper.GetShortFileName(imgX.Folder, imgX.Hash);
                        backgroundworker.ReportProgress(0, $"[{age} ago] {shortfilename}");
                        imgX.SetNext(hashes[mode]);
                    }

                    if (mode == 2) {
                        imgX.SetDistance(mindistances[2]);
                    }
                }

                imgX.SetLastCheck(DateTime.Now);


 */
