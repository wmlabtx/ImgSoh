using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ImgSoh
{
    public static class AppImgs
    {
        private static readonly object _lock = new object();
        private static readonly SortedList<string, Img> _imgList = new SortedList<string, Img>();
        private static readonly SortedList<string, Img> _nameList = new SortedList<string, Img>();

        public static void Clear()
        {
            lock (_lock) {
                _imgList.Clear();
                _nameList.Clear();
            }
        }

        public static int Count()
        {
            int count;
            lock (_lock) {
                if (_imgList.Count != _nameList.Count) {
                    throw new Exception();
                }

                count = _imgList.Count;
            }

            return count;
        }

        public static bool TryGetImg(string key, out Img img)
        {
            bool result;
            img = null;
            lock (_lock) {
                result = key.Length == 32 ? 
                    _imgList.TryGetValue(key, out img) : 
                    _nameList.TryGetValue(key, out img);
            }

            return result;
        }

        public static void Add(Img img)
        {
            lock (_lock) {
                _imgList.Add(img.Hash, img);
                _nameList.Add(img.Name, img);
            }
        }

        public static void Remove(string key)
        {
            lock (_lock) {
                if (key.Length == 32) {
                    if (_imgList.ContainsKey(key)) {
                        _nameList.Remove(_imgList[key].Name);
                        _imgList.Remove(key);
                    }
                }
                else {
                    if (_nameList.ContainsKey(key)) {
                        _imgList.Remove(_nameList[key].Hash);
                        _nameList.Remove(key);
                    }
                }
            }
        }

        private static bool IsValid(Img imgX)
        {
            lock (_lock) {
                if (imgX.Magnitude <=0f ||
                    string.IsNullOrWhiteSpace(imgX.Next) ||
                    (!string.IsNullOrWhiteSpace(imgX.Horizon) &&
                     !string.IsNullOrWhiteSpace(imgX.Next) &&
                     string.CompareOrdinal(imgX.Horizon, imgX.Next) >= 0)
                   ) {
                    return false;
                }

                var next = imgX.Next.Substring(4);
                if (!TryGetImg(next, out _)) {
                    return false;
                }
            }

            return true;
        }

        public static string GetNextCheck()
        {
            Img bestImgX = null;
            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.ToArray();
            }

            foreach (var imgX in imgArray) {
                if (!IsValid(imgX)) {
                    bestImgX = imgX;
                    break;
                }

                if (bestImgX == null || imgX.LastCheck < bestImgX.LastCheck) {
                    bestImgX = imgX;
                }
            }

            var hash = bestImgX?.Hash;
            return hash;
        }

        public static void GetNextView(out string bestHash, out string status)
        {
            bestHash = null;
            status = null;
            var counter = 0;
            Img bestImg = null;


            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.ToArray();
            }

            foreach (var img in imgArray) {
                if (!IsValid(img)) {
                    continue;
                }

                if (bestImg == null) {
                    bestImg = img;
                    counter = 1;
                    continue;
                }

                if (!img.Verified && bestImg.Verified) {
                    bestImg = img;
                    counter = 1;
                    continue;
                }

                if (img.Verified && !bestImg.Verified) {
                    continue;
                }

                if (img.Counter < bestImg.Counter) {
                    bestImg = img;
                    counter = 1;
                    continue;
                }

                if (img.Counter > bestImg.Counter) {
                    continue;
                }

                if (string.CompareOrdinal(img.Next.Substring(0, 2), bestImg.Next.Substring(0, 2)) < 0) {
                    bestImg = img;
                    counter = 1;
                    continue;
                }

                if (string.CompareOrdinal(img.Next.Substring(0, 2), bestImg.Next.Substring(0, 2)) > 0) {
                    continue;
                }

                counter++;
            }

            var total = Count();
            bestHash = bestImg?.Hash;
            status = $"{bestImg?.Next.Substring(0, 2)}:{counter}/{total}";
        }

        /*
        public static void GetNextView(out string bestHash, out string status)
        {
            bestHash = null;
            status = null;
            var prefix = new[] { "+", "0", "#" };
            var counters = new[] { 0, 0, 0 };
            var candidates = new Img[] { null, null, null };
            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.ToArray();
            }

            var zeros = 0;
            foreach (var img in imgArray) {
                if (!IsValid(img)) {
                    continue;
                }

                if (img.Counter == 0) {
                    zeros++;
                }

                int category;
                if (!img.Verified) {
                    category = 0;
                }
                else {
                    category = img.Next.StartsWith("00") ? 1 : 2;
                }

                counters[category]++;
                if (candidates[category] == null || img.LastView < candidates[category].LastView) {
                    candidates[category] = img;
                }
            }

            if (candidates[0] == null && candidates[1] == null && candidates[2] == null) {
                status = "no candidates";
                return;
            }

            var total = Count();
            do {
                var rindex = AppVars.RandomNext(3);
                if (candidates[rindex] != null) {
                    bestHash = candidates[rindex].Hash;
                }
                
                status = $"{prefix[rindex]}:{counters[rindex]}/{zeros}/{total}";
            } while (bestHash == null);
        }
        */

        public static IEnumerable<Img> GetFamily(int family)
        {
            if (family <= 0) {
                return Array.Empty<Img>();
            }

            Img[] array;
            lock (_lock) {
                array = _imgList.Where(e => e.Value.Family == family && IsValid(e.Value)).Select(e => e.Value).ToArray();
            }

            return array;
        }

        public static void RenameFamily(int oldfamily, int newfamily)
        {
            lock (_lock) {
                var fo = GetFamily(oldfamily);
                foreach (var img in fo) {
                    AppDatabase.SetFamily(img.Hash, newfamily);
                }
            }
        }

        public static int GetNewFamily()
        {
            int[] families;
            lock (_lock) {
                families = _imgList
                    .Where(e => e.Value.Family > 0)
                    .Select(e => e.Value.Family)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToArray();
            }

            if (families.Length == 0) {
                return 1;
            }

            var pos = 0;
            while (pos < families.Length) {
                if (families[pos] != pos + 1) {
                    break;
                }

                pos++;
            }

            return pos + 1;
        }

        public static void Find(Img imgX, out string radiusNext, out int counter)
        {
            Img[] copy;
            lock (_lock) {
                copy = _imgList.Values.ToArray();
            }

            var distances = new float[copy.Length];
            var vx = imgX.GetVector();
            var mx = imgX.Magnitude;
            Parallel.For(0, copy.Length, i => {
                distances[i] = AppVit.GetDistance(vx, mx, copy[i].GetVector(), copy[i].Magnitude);
            });

            radiusNext = null;
            counter = 0;
            for (var i = 0; i < copy.Length; i++) {
                if (imgX.Hash.Equals(copy[i].Hash)) {
                    continue;
                }

                var radius = Helper.GetRadius(copy[i].Hash, distances[i]);
                if (string.IsNullOrEmpty(imgX.Horizon) || (!string.IsNullOrEmpty(imgX.Horizon) &&
                                                           string.CompareOrdinal(radius, imgX.Horizon) > 0)) {
                    if (radiusNext == null || string.CompareOrdinal(radius, radiusNext) < 0) {
                        radiusNext = radius;
                    }
                }

                if (!string.IsNullOrEmpty(imgX.Horizon) && string.CompareOrdinal(radius, imgX.Horizon) <= 0) {
                    counter++;
                }
            }
        }

        public static string GetName(string hash)
        {
            string name;
            var length = 5;
            do {
                length++;
                name = hash.Substring(0, length);
            } while (TryGetImg(name, out _));

            return name;
        }
    }
}
