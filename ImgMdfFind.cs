using MetadataExtractor;
using System;
using System.IO;

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

                /*
                var victim = ChooseVictim(imgX, imgY);
                AppPanels.SetVictim(victim);
                */

                //if (victim != 0 && victim != 1) {
                    var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                    var shortfilename = Helper.GetShortFileName(imgX.Path, hashX);
                    var distance = VitHelper.GetDistance(imgX.GetVector(), imgY.GetVector());
                    progress?.Report($"{status} [{age} ago] {shortfilename} {distance:F4}");
                    break;
                    /*
                }
                else {
                    var imgs = new Img[2];
                    imgs[0] = imgX;
                    imgs[1] = imgY;
                    var shortfilename = Helper.GetShortFileName(imgX.Path, hashX);
                    progress?.Report($"{status} DELETE {shortfilename}");
                    Delete(imgs[victim].Hash);
                    hashX = null;
                }
                    */
            }
            while (true);
        }

        private static int ChooseVictim(Img imgX, Img imgY)
        {
            var imgs = new Img[2];
            imgs[0] = imgX;
            imgs[1] = imgY;
            var panels = new ImgPanel[2];
            panels[0] = AppPanels.GetImgPanel(0);
            panels[1] = AppPanels.GetImgPanel(1);
            var diff = BitmapHelper.BitmapDiff(panels[0].Bitmap, panels[1].Bitmap);
            if (!diff) {
                return -1;
            }

            var sizes = new long[2];
            sizes[0] = new FileInfo(Helper.GetFileName(imgX.Path, imgX.Hash, imgX.Ext)).Length;
            sizes[1] = new FileInfo(Helper.GetFileName(imgY.Path, imgY.Hash, imgY.Ext)).Length;
            for (var i = 0; i < 2; i++) {
                if (imgs[i].Taken == imgs[1 - i].Taken && imgs[i].Meta == imgs[1 - i].Meta && sizes[i] <= sizes[1]) {
                    return i;
                }
            }

            for (var i = 0; i < 2; i++) {
                if (imgs[i].Taken == imgs[1 - i].Taken && imgs[i].Meta != imgs[1 - i].Meta) {
                    if (imgs[i].Meta == 11) {
                        return 1 - i;
                    }

                    if (imgs[i].Meta == 6) {
                        return i;
                    }

                    if (imgs[i].Meta == 0 && imgs[1 - i].Meta != 6) {
                        return i;
                    }

                    if (imgs[i].Meta < imgs[1 - i].Meta) {
                        return i;
                    }
                }
            }

            for (var i = 0; i < 2; i++) {
                if (imgs[i].Taken != imgs[1 - i].Taken && imgs[i].Meta == imgs[1 - i].Meta) {
                    if (imgs[i].Taken == DateTime.MinValue) {
                        return i;
                    }

                    if (imgs[i].Taken != DateTime.MinValue && imgs[i].Taken < imgs[1 - i].Taken) {
                        return i;
                    }
                }
            }

            for (var i = 0; i < 2; i++) {
                if (imgs[i].Taken == DateTime.MinValue && imgs[1 - i].Taken != DateTime.MinValue) {
                    return i;
                }

                if (imgs[i].Meta == 11) {
                    return 1 - i;
                }

                if (imgs[i].Meta == 6) {
                    return i;
                }

                if (imgs[i].Taken != DateTime.MinValue && imgs[1 - i].Taken != DateTime.MinValue && imgs[i].Taken < imgs[1 - i].Taken) {
                    return 1 - i;
                }
            }

            return -1;
        }
    }
}
