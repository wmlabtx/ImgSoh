using ImageMagick;
using System;
using System.Linq;

namespace ImgSoh
{
    public static partial class ImgMdf
    {
        public static void Find(string hashX, IProgress<string> progress)
        {
            Img imgX = null;
            do {
                var totalcount = AppImgs.Count();
                if (totalcount < 2) {
                    progress?.Report($"totalcount = {totalcount}");
                    return;
                }

                if (!string.IsNullOrEmpty(hashX) && !AppImgs.TryGet(hashX, out imgX)) {
                    hashX = null;
                }

                if (imgX == null) {
                    imgX = AppImgs.GetForView();
                    hashX = imgX.Hash;
                }

                if (!AppPanels.SetImgPanel(0, hashX)) {
                    Delete(hashX);
                    hashX = null;
                    continue;
                }

                var hashY = imgX.Next.Substring(4);
                if (!AppImgs.TryGet(hashY, out var imgY)) {
                    hashX = null;
                    continue;
                }

                if (!AppPanels.SetImgPanel(1, hashY)) {
                    hashX = null;
                    continue;
                }

                var diff = 255;
                var distance = AppVit.GetDistance(imgX.Vector, imgX.Magnitude, imgY.Vector, imgY.Magnitude);
                if (distance < 0.25f) {
                    GetVictim(imgX, imgY, out var victim, out diff);
                    AppPanels.SetVictim(victim);
                }

                var total = AppImgs.Count();
                progress?.Report($"{total} D={distance:F4}  F{diff}");
                break;
            }
            while (true);
        }

        private static int GetVictim(Img x, Img y)
        {
            var imgs = new[] { x, y };
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
