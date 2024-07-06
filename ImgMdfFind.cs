using ImageMagick;
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
                var totalcount = AppImgs.Count();
                if (totalcount < 2) {
                    progress?.Report($"totalcount = {totalcount}");
                    return;
                }

                if (hashX == null) {
                    AppImgs.GetNextView(out var hash, out status);
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

                if (!AppImgs.TryGetImg(hashX, out var imgX)) {
                    Delete(hashX);
                    hashX = null;
                    continue;
                }

                var hashY = imgX.Next.Substring(4);
                if (hashX.Equals(hashY)) {
                    throw new Exception();
                }

                if (!AppPanels.SetImgPanel(1, hashY)) {
                    Delete(hashY);
                    hashX = null;
                    continue;
                }

                if (!AppImgs.TryGetImg(hashY, out var imgY)) {
                    Delete(hashY);
                    hashX = null;
                    continue;
                }

                var diff = 255;
                var distance = AppVit.GetDistance(imgX.GetVector(), imgX.Magnitude, imgY.GetVector(), imgY.Magnitude);
                if (distance < 0.25f) {
                    GetVictim(imgX, imgY, out var victim, out diff);
                    AppPanels.SetVictim(victim);
                }

                var age = Helper.TimeIntervalToString(DateTime.Now.Subtract(imgX.LastView));
                progress?.Report($"{status} [{age} ago] {imgX.Name} {distance:F4} D{diff}");
                break;
            }
            while (true);
        }

        private static int GetVictim(Img x, Img y)
        {
            var imgs = new Img[] { x, y };
            var px = AppPanels.GetImgPanel(0);
            var py = AppPanels.GetImgPanel(1);
            var panels = new[] { px, py };
            if (px.Bitmap.Width == py.Bitmap.Width && px.Bitmap.Height == py.Bitmap.Height) {
                for (var i = 0; i < 2; i++) {
                    if (imgs[i].Meta == 11 && imgs[1 - i].Meta != 11) {
                         return 1 - i;
                    }
                }

                for (var i = 0; i < 2; i++) {
                    if (imgs[i].Taken == imgs[1 - i].Taken && imgs[i].Meta == imgs[1 - i].Meta && panels[i].Size <= panels[1 - i].Size) {
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
            }

            return -1;
        }

        private static void GetVictim(Img x, Img y, out int victim, out int diff)
        {
            victim = -1;
            diff = 255;
            var xf = AppFile.GetFileName(x.Name, AppConsts.PathHp);
            var xd = AppFile.ReadEncryptedFile(xf);
            var xm = AppBitmap.ImageDataToMagickImage(xd);
            var yf = AppFile.GetFileName(y.Name, AppConsts.PathHp);
            var yd = AppFile.ReadEncryptedFile(yf);
            var ym = AppBitmap.ImageDataToMagickImage(yd);
            var g = new MagickGeometry {
                Width = 128,
                Height = 128,
                IgnoreAspectRatio = true
            };
            xm.Grayscale();
            xm.Resize(g);
            ym.Grayscale();
            ym.Resize(g);
            var zm = new MagickImage(xm);
            zm.Composite(ym, CompositeOperator.Difference);
            var p = zm.GetPixels();
            var raw = p.ToByteArray(PixelMapping.RGB);
            if (raw != null) {
                diff = raw.Max();
                if (diff < 32) {
                    victim = GetVictim(x, y);
                }
            }

            zm.Dispose();
            ym.Dispose();
            xm.Dispose();
        }
    }
}
