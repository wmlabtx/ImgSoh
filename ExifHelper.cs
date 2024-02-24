using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;

namespace ImgSoh
{
    public static class ExifHelper
    {
        public static string[] GetFingerPrint(byte[] imagedata)
        {
            var fplist = new SortedSet<string>();
            using (var ms = new MemoryStream(imagedata)) {
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(ms);
                foreach (var directory in directories) {
                    foreach (var tag in directory.Tags) {
                        var e = $"{directory.Name}-{tag.Name}={tag.Description}";
                        fplist.Add(e);
                    }
                }
            }

            return fplist.ToArray();
        }

        public static short GetMatch(string[] x, string[] y)
        {
            short result = 0; 
            var i = 0;
            var j = 0;
            while (i < x.Length && j < y.Length) {
                var c = string.CompareOrdinal(x[i], y[j]);
                if (c == 0) {
                    result++;
                    i++;
                    j++;
                }
                else {
                    if (c < 0) {
                        i++;
                    }
                    else {
                        j++;
                    }
                }
            }

            return result;
        }
    }
}
