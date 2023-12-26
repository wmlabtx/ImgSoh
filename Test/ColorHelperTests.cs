using System;
using System.Diagnostics;
using System.IO;
using ImgSoh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ColorHelperTests
    {
        [TestMethod]
        public void Range()
        {
            double lmin = double.MaxValue, lmax = double.MinValue;
            double amin = double.MaxValue, amax = double.MinValue;
            double bmin = double.MaxValue, bmax = double.MinValue;
            for (var rb = 0; rb < 256; rb++) {
                for (var gb = 0; gb < 256; gb++) {
                    for (var bb = 0; bb < 256; bb++) {
                        ColorHelper.RgbToLab(rb, gb, bb, out var l, out var a, out var b);
                        lmin = Math.Min(lmin, l);
                        lmax = Math.Max(lmax, l);
                        amin = Math.Min(amin, a);
                        amax = Math.Max(amax, a);
                        bmin = Math.Min(bmin, b);
                        bmax = Math.Max(bmax, b);
                    }
                }
            }

            Debug.Print($"lmin = {lmin:F10}");
            Debug.Print($"lmax = {lmax:F10}");
            Debug.Print($"amin = {amin:F10}");
            Debug.Print($"amax = {amax:F10}");
            Debug.Print($"as = {amax - amin:F10}");
            Debug.Print($"bmin = {bmin:F10}");
            Debug.Print($"bmax = {bmax:F10}");
            Debug.Print($"bs = {bmax - bmin:F10}");

            /*
lmin = 0.0000000000
lmax = 0.9999999935
amin = -0.2338875742
amax = 0.2762167535
as = 0.5101043277
bmin = -0.3115281477
bmax = 0.1985697547
bs = 0.5100979023
            */
        }

        [TestMethod]
        public void QuantRange()
        {
            int lmin = int.MaxValue, lmax = int.MinValue;
            int amin = int.MaxValue, amax = int.MinValue;
            int bmin = int.MaxValue, bmax = int.MinValue;
            for (var rb = 0; rb < 256; rb++) {
                for (var gb = 0; gb < 256; gb++) {
                    for (var bb = 0; bb < 256; bb++) {
                        ColorHelper.RgbToQuantLab(rb, gb, bb, out int li, out int ai, out int bi);
                        lmin = Math.Min(lmin, li);
                        lmax = Math.Max(lmax, li);
                        amin = Math.Min(amin, ai);
                        amax = Math.Max(amax, ai);
                        bmin = Math.Min(bmin, bi);
                        bmax = Math.Max(bmax, bi);
                    }
                }
            }

            Debug.Print($"lmin = {lmin}");
            Debug.Print($"lmax = {lmax}");
            Debug.Print($"amin = {amin}");
            Debug.Print($"amax = {amax}");
            Debug.Print($"bmin = {bmin}");
            Debug.Print($"bmax = {bmax}");

            /*
    lmin = 0.0000000000
    lmax = 0.9999999935
    amin = -0.2338875742
    amax = 0.2762167535
    as = 0.5101043277
    bmin = -0.3115281477
    bmax = 0.1985697547
    bs = 0.5100979023
            */
        }

        [TestMethod]
        public void CreateTable()
        {
            var table = new byte[256 * 256 * 256 * 3];
            for (var rb = 0; rb < 256; rb++) {
                for (var gb = 0; gb < 256; gb++) {
                    for (var bb = 0; bb < 256; bb++) {
                        var offset = ((rb << 16) | (gb << 8) | bb) * 3;
                        ColorHelper.RgbToQuantLab(rb, gb, bb, out int li, out int ai, out int bi);
                        table[offset] = (byte)li;
                        table[offset + 1] = (byte)ai;
                        table[offset + 2] = (byte)bi;
                    }
                }
            }

            File.WriteAllBytes(AppConsts.FileRgbLab, table);
        }

        [TestMethod]
        public void CheckExif()
        {
            /*
            const string resultfile = "d:\\Users\\Murad\\Documents\\ImgSoh\\Test\\bin\\Debug\\Exif\\result.json";
            var content = File.ReadAllText(resultfile);
            if (content.StartsWith("{ready0000}\r\n")) {
                content = content.Substring("{ready0000\r\n}".Length);
                content = content.Replace(@"[{", @"{").Replace(@"}]", @"}");
            }

            var o1 = JObject.Parse(content);
            foreach (var jtocken in o1) {
                var name = jtocken.Key;
                var value = jtocken.Value;
                if (value.HasValues) {
                    var array = string.Join(" ", value.Values<string>().ToArray());
                    Debug.WriteLine($"{name} {array} (...array...)");
                }
                else {
                    var svalue = value.ToString();
                    if (svalue.Length < 256) {
                        Debug.WriteLine($"{name} {svalue}");
                    }
                    else {
                        var buffer = Encoding.UTF8.GetBytes(svalue);
                        var sb = new StringBuilder();
                        using (var md5 = MD5.Create()) {
                            var hashMD5 = md5.ComputeHash(buffer);

                            foreach (var b in hashMD5) {
                                sb.Append($"{b:x2}");
                            }
                        }

                        svalue = sb.ToString();
                        Debug.WriteLine($"{name} {svalue} (...binary...)");
                    }
                }
            }
            */

            /*
            const string resultfile = "d:\\Users\\Murad\\Documents\\ImgSoh\\Test\\bin\\Debug\\Exif\\result.xml";
            var content = File.ReadAllText(resultfile);
            if (content.StartsWith("{ready0000}\r\n")) {
                content = content.Substring("{ready0000\r\n}".Length);
            }

            var byteArray = Encoding.UTF8.GetBytes(content);
            using (var ms = new MemoryStream(byteArray)) {
                var xml = XDocument.Load(ms);
                foreach (var e in xml.DescendantNodes()) {
                    if (e is XElement el) {
                        if (!el.IsEmpty && el.NodeType == XmlNodeType.Element) {
                            if (!el.Name.Equals("rdf:RDF")) {
                                var xpath = string.Join("/", el.AncestorsAndSelf().Reverse().Select(a => a.Name.LocalName).ToArray());
                                Debug.WriteLine($"{xpath} {el.Name} {el.Value}");
                            }

                        }
                    }
                    
                }
            }
            */

            const string filename = "d:\\Users\\Murad\\Documents\\ImgSoh\\Test\\bin\\Debug\\Exif\\Page26-Exif-IPTC.jpg";
            //const string filename = "d:\\Users\\Murad\\Documents\\ImgSoh\\Test\\bin\\Debug\\Exif\\Wednesday_Christmas_final-NoExif.jpg";
            //const string filename = "d:\\Users\\Murad\\Documents\\ImgSoh\\Test\\bin\\Debug\\Exif\\ml38a001.jpg";
            //const string filename = "d:\\Users\\Murad\\Documents\\ImgSoh\\Test\\bin\\Debug\\Exif\\Alice Club 9205-171-Exif.JPG";
            var imgdata = File.ReadAllBytes(filename);
            ExifHelper.Start();
            var result = ExifHelper.GetFingerPrint("test", imgdata);
            foreach (var e in result) {
                Debug.WriteLine($"{e.Key} {e.Value}");
            }

            ExifHelper.Stop();

            /*
             var imagedata = //File.ReadAllBytes("d:\\Users\\Murad\\Documents\\ImgSoh\\Test\\bin\\Debug\\Exif\\Page26-Exif-IPTC.jpg");
            File.ReadAllBytes("d:\\Users\\Murad\\Documents\\ImgSoh\\Test\\bin\\Debug\\Exif\\Wednesday_Christmas_final-NoExif.jpg");
        using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata)) {
            var exif = magickImage.GetExifProfile();
            if (exif != null) {
                foreach (var value in exif.Values) {
                    var str = value.ToString();
                    Debug.WriteLine($"{value.Tag} {value.DataType} {str}");
                }
            }
            */
        }
    }

        /*

ImageWidth Short 2480
ImageLength Short 3508
BitsPerSample Short ImageMagick.ExifShortArray
PhotometricInterpretation Short RGB
Orientation Short Horizontal (normal)
SamplesPerPixel Short 3
XResolution Rational 720000/10000
YResolution Rational 720000/10000
ResolutionUnit Short Inches
Software String Adobe Photoshop 24.7 (Windows)
DateTime String 2023:10:06 15:21:59
ExifVersion Undefined ImageMagick.ExifByteArray
ColorSpace Short sRGB
PixelXDimension Short 1000
PixelYDimension Short 1415

         */
        /*
        [TestMethod]
        public void RgbToLabFastTest()
        {
            ColorHelper.LoadTable(null);
            ColorHelper.RgbToQuantLab(0, 100, 00, out int li1, out int ai1, out int bi1);
            ColorHelper.RgbToLabFast(0, 100, 00, out int li2, out int ai2, out int bi2);
        }
        */

        /*
        [TestMethod]
        public void GetDistance()
        {
            ColorHelper.LoadTable(null);
            var images = new[] {
                "gab_org", "gab_bw", "gab_scale", "gab_flip", "gab_r90", "gab_crop", "gab_toside",
                "gab_blur", "gab_exp", "gab_logo", "gab_noice", "gab_r3", "gab_r10",
                "gab_face", "gab_sim1", "gab_sim2",
                "gab_nosim1", "gab_nosim2", "gab_nosim3", "gab_nosim4", "gab_nosim5", "gab_nosim6"
            };

            var vectors = new List<Tuple<string, byte[]>>();
            for (var i = 1; i <= 73; i++) {
                var name = $"DataSet2\\lexie-179-{i:D3}.jpg";
                var imagedata = File.ReadAllBytes(name);
                using (var magickImage = BitmapHelper.ImageDataToMagickImage(imagedata))
                using (var bitmap = BitmapHelper.MagickImageToBitmap(magickImage, RotateFlipType.RotateNoneFlipNone)) {
                    var vector = ColorHelper.CalculateVector(bitmap);
                    vectors.Add(new Tuple<string, byte[]>(name, vector));
                }
            }

            for (var i = 0; i < vectors.Count; i++) {
                var distance = ColorHelper.GetDistance(vectors[0].Item2, vectors[i].Item2);
                Debug.WriteLine($"{vectors[i].Item1} = {distance:F4}");
            }
        }
        */
}
