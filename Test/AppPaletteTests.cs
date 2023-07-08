using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Test
{
    [TestClass]
    public class PaletteTests
    {
        [TestMethod]
        public void CreatePalette()
        {
            AppPalette.Create();
        }

        [TestMethod]
        public void LearnPalette()
        {
            Debug.WriteLine("Loading database");
            AppDatabase.LoadImages(null);
            var counter = 0;
            while (counter < 100) {
                var error = AppPalette.Learn();
                Debug.WriteLine($"{counter}: {error:F4}");
                counter++;
                if (error < 0.0001f) {
                    break;
                }
            }
        }

        [TestMethod]
        public void GetDistance()
        {
            Debug.WriteLine("Loading database");
            AppDatabase.LoadImages(null);
            var images = new[] {
                "gab_org", "gab_bw", "gab_scale", "gab_flip", "gab_r90", "gab_crop", "gab_toside",
                "gab_blur", "gab_exp", "gab_logo", "gab_noice", "gab_r3", "gab_r10",
                "gab_face", "gab_sim1", "gab_sim2",
                "gab_nosim1", "gab_nosim2", "gab_nosim3", "gab_nosim4", "gab_nosim5", "gab_nosim6"
            };

            var vectors = new Tuple<string, float[]>[images.Length];
            for (var i = 0; i < images.Length; i++) {
                var name = $"AppPalette\\{images[i]}.jpg";
                var imagedata = File.ReadAllBytes(name);
                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    var hist = AppPalette.ComputeHistogram(bitmap);
                    vectors[i] = new Tuple<string, float[]>(name, hist);
                }
            }

            for (var i = 0; i < vectors.Length; i++) {
                var distance = AppPalette.GetDistance(vectors[0].Item2, vectors[i].Item2);
                Debug.WriteLine($"{images[i]} = {distance:F2}");
            }

            /*
                gab_org = 0.00
                gab_bw = 0.18
                gab_scale = 0.00
                gab_flip = 0.00
                gab_r90 = 0.00
                gab_crop = 0.06
                gab_toside = 0.18
                gab_blur = 0.09
                gab_exp = 0.10
                gab_logo = 0.00
                gab_noice = 0.10
                gab_r3 = 0.05
                gab_r10 = 0.11
                gab_face = 0.23
                gab_sim1 = 0.13
                gab_sim2 = 0.31
                gab_nosim1 = 0.65
                gab_nosim2 = 0.22
                gab_nosim3 = 0.58
                gab_nosim4 = 0.38
                gab_nosim5 = 0.17
                gab_nosim6 = 0.56
             
             */
        }

        [TestMethod]
        public void GetDistance2()
        {
            Debug.WriteLine("Loading database");
            AppDatabase.LoadImages(null);
            var vectors = new Tuple<string, float[]>[73];
            for (var i = 1; i <= vectors.Length; i++) {
                var name = $"AppPalette2\\lexie-179-{i:D3}.jpg";
                var imagedata = File.ReadAllBytes(name);
                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    var hist = AppPalette.ComputeHistogram(bitmap);
                    vectors[i - 1] = new Tuple<string, float[]>(name, hist);
                }
            }

            for (var i = 0; i < vectors.Length; i++) {
                var distance = AppPalette.GetDistance(vectors[0].Item2, vectors[i].Item2);
                Debug.WriteLine($"{vectors[i].Item1} = {distance:F2}");
            }

            /*
AppPalette2\lexie-179-001.jpg = 0.00
AppPalette2\lexie-179-002.jpg = 0.33
AppPalette2\lexie-179-003.jpg = 0.31
AppPalette2\lexie-179-004.jpg = 0.32
AppPalette2\lexie-179-005.jpg = 0.33
AppPalette2\lexie-179-006.jpg = 0.34
AppPalette2\lexie-179-007.jpg = 0.28
AppPalette2\lexie-179-008.jpg = 0.32
AppPalette2\lexie-179-009.jpg = 0.27
AppPalette2\lexie-179-010.jpg = 0.30
AppPalette2\lexie-179-011.jpg = 0.24
AppPalette2\lexie-179-012.jpg = 0.30
AppPalette2\lexie-179-013.jpg = 0.24
AppPalette2\lexie-179-014.jpg = 0.28
AppPalette2\lexie-179-015.jpg = 0.21
AppPalette2\lexie-179-016.jpg = 0.22
AppPalette2\lexie-179-017.jpg = 0.30
AppPalette2\lexie-179-018.jpg = 0.14
AppPalette2\lexie-179-019.jpg = 0.30
AppPalette2\lexie-179-020.jpg = 0.26
AppPalette2\lexie-179-021.jpg = 0.19
AppPalette2\lexie-179-022.jpg = 0.19
AppPalette2\lexie-179-023.jpg = 0.12
AppPalette2\lexie-179-024.jpg = 0.18
AppPalette2\lexie-179-025.jpg = 0.21
AppPalette2\lexie-179-026.jpg = 0.15
AppPalette2\lexie-179-027.jpg = 0.10
AppPalette2\lexie-179-028.jpg = 0.11
AppPalette2\lexie-179-029.jpg = 0.12
AppPalette2\lexie-179-030.jpg = 0.18
AppPalette2\lexie-179-031.jpg = 0.33
AppPalette2\lexie-179-032.jpg = 0.28
AppPalette2\lexie-179-033.jpg = 0.25
AppPalette2\lexie-179-034.jpg = 0.35
AppPalette2\lexie-179-035.jpg = 0.25
AppPalette2\lexie-179-036.jpg = 0.27
AppPalette2\lexie-179-037.jpg = 0.25
AppPalette2\lexie-179-038.jpg = 0.23
AppPalette2\lexie-179-039.jpg = 0.21
AppPalette2\lexie-179-040.jpg = 0.16
AppPalette2\lexie-179-041.jpg = 0.25
AppPalette2\lexie-179-042.jpg = 0.18
AppPalette2\lexie-179-043.jpg = 0.19
AppPalette2\lexie-179-044.jpg = 0.22
AppPalette2\lexie-179-045.jpg = 0.28
AppPalette2\lexie-179-046.jpg = 0.24
AppPalette2\lexie-179-047.jpg = 0.24
AppPalette2\lexie-179-048.jpg = 0.11
AppPalette2\lexie-179-049.jpg = 0.19
AppPalette2\lexie-179-050.jpg = 0.23
AppPalette2\lexie-179-051.jpg = 0.23
AppPalette2\lexie-179-052.jpg = 0.17
AppPalette2\lexie-179-053.jpg = 0.24
AppPalette2\lexie-179-054.jpg = 0.24
AppPalette2\lexie-179-055.jpg = 0.27
AppPalette2\lexie-179-056.jpg = 0.28
AppPalette2\lexie-179-057.jpg = 0.25
AppPalette2\lexie-179-058.jpg = 0.15
AppPalette2\lexie-179-059.jpg = 0.18
AppPalette2\lexie-179-060.jpg = 0.16
AppPalette2\lexie-179-061.jpg = 0.16
AppPalette2\lexie-179-062.jpg = 0.19
AppPalette2\lexie-179-063.jpg = 0.15
AppPalette2\lexie-179-064.jpg = 0.16
AppPalette2\lexie-179-065.jpg = 0.23
AppPalette2\lexie-179-066.jpg = 0.24
AppPalette2\lexie-179-067.jpg = 0.28
AppPalette2\lexie-179-068.jpg = 0.23
AppPalette2\lexie-179-069.jpg = 0.15
AppPalette2\lexie-179-070.jpg = 0.28
AppPalette2\lexie-179-071.jpg = 0.36
AppPalette2\lexie-179-072.jpg = 0.31
AppPalette2\lexie-179-073.jpg = 0.21
             
             */
        }

    }
}
