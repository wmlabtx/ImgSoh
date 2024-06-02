using System;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Find(string hashX, IProgress<string> progress)
        {
            var status = string.Empty;
            do {
                var totalcount = AppDatabase.ImgCount();
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
                    Delete(hashX);
                    hashX = null;
                    continue;
                }

                if (!AppDatabase.TryGetImg(hashX, out var imgX)) {
                    Delete(hashX);
                    hashX = null;
                    continue;
                }

                var hashY = AppDatabase.GetHashY(hashX);
                if (hashX.Equals(hashY)) {
                    throw new Exception();
                }

                if (!AppPanels.SetImgPanel(1, hashY)) {
                    Delete(hashY);
                    hashX = null;
                    continue;
                }

                if (!AppDatabase.TryGetImg(hashY, out var imgY)) {
                    Delete(hashY);
                    hashX = null;
                    continue;
                }

                var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                var shortfilename = Helper.GetShortFileName(imgX.Path, hashX);
                var distance = VitHelper.GetDistance(imgX.GetVector(), imgY.GetVector());
                progress.Report($"{status} [{age} ago] {shortfilename} {distance:F4}");

                /*
                var panelX = AppPanels.GetImgPanel(0);
                var panelY = AppPanels.GetImgPanel(1);
                if (distance < 0.002f &&
                    panelX.Bitmap.Width == panelY.Bitmap.Width &&
                    panelX.Bitmap.Height == panelY.Bitmap.Height) {
                    var indexToDelete = -1;
                    if (imgX.Taken != imgY.Taken) {
                        if (imgX.Taken == DateTime.MinValue && imgY.Taken != DateTime.MinValue) {
                            indexToDelete = 0;
                        }
                        else {
                            if (imgX.Taken != DateTime.MinValue && imgY.Taken == DateTime.MinValue) {
                                indexToDelete = 1;
                            }
                            else {
                                if (imgX.Taken < imgY.Taken) {
                                    indexToDelete = 0;
                                }
                                else {
                                    indexToDelete = 1;
                                }
                            }
                        }
                    }
                    else {
                        if (imgX.Meta != imgY.Meta) {
                            if (imgX.Meta == 6 && imgY.Meta != 6) {
                                indexToDelete = 0;
                            }
                            else {
                                if (imgX.Meta != 6 && imgY.Meta == 6) {
                                    indexToDelete = 1;
                                }
                                else {
                                    if (imgX.Meta == 0 && imgY.Meta > 0) {
                                        indexToDelete = 0;
                                    }
                                    else {
                                        if (imgX.Meta > 0 && imgY.Meta == 0) {
                                            indexToDelete = 1;
                                        }
                                        else {
                                            if (imgX.Meta < imgY.Meta) {
                                                indexToDelete = 0;
                                            }
                                            else {
                                                indexToDelete = 1;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else {
                            if (panelX.Size <= panelY.Size) {
                                indexToDelete = 0;
                            }
                            else {
                                indexToDelete = 1;
                            }
                        }
                    }

                    var panelD = AppPanels.GetImgPanel(indexToDelete);
                    progress?.Report($"Delete {indexToDelete}:{panelD.Hash}");
                    Delete(panelD.Hash);
                    hashX = null;
                    continue;
                }
                */

                break;
            }
            while (true);
        }
    }
}
