using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
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
            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.Where(IsValid).OrderBy(e => e.LastView).ToArray();
            }

            bestHash = imgArray[0].Hash;
            var total = Count();
            status = $"{total}";
        }

        public static void SetLastView(Img img)
        {
            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.OrderBy(e => e.LastView).ToArray();
            }

            var pos = img.Counter * imgArray.Length / 100 + 1;
            var lastview = DateTime.Now;
            if (pos < imgArray.Length) {
                var dt1 = imgArray[pos].LastView;
                var dt2 = imgArray[pos - 1].LastView;
                lastview = DateTime.FromBinary((dt1.Ticks + dt2.Ticks) / 2);
            }

            AppDatabase.SetLastView(img.Hash, lastview);
        }
 
        /*
        public static void GetNextView(out string bestHash, out string status)
        {
            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.Where(IsValid).ToArray();
            }


            var list = new SortedList<int, Img>();
            foreach (var img in imgArray) {
                var key = img.Verified ? img.Family : -1;
                if (list.TryGetValue(key, out var value)) {
                    if (img.LastView < value.LastView) {
                        list[key] = img;
                    }
                }
                else {
                    list.Add(key, img);
                }
            }

            var array = list.ToArray();
            var irandom = AppVars.RandomNext(array.Length);
            bestHash = array[irandom].Value.Hash;
            var total = Count();
            status = $"{array[irandom].Key}/{total}";
        }
        */

        /*
        public static void GetNextView(out string bestHash, out string status)
        {
            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.Where(IsValid).ToArray();
            }

            var list = new SortedList<int, List<Img>>();
            foreach (var img in imgArray) {
                var key = Math.Min(9, !img.Verified ? -1 : img.Counter);
                if (list.TryGetValue(key, out var value)) {
                    value.Add(img);
                }
                else {
                    list.Add(key, new List<Img> {img});
                }
            }

            var array = list.Where(e => e.Value.Count >= 100).ToArray();
            var ikey = AppVars.RandomNext(array.Length);
            var minlv = array[ikey].Value.Min(e => e.LastView);
            bestHash = array[ikey].Value.First(e => e.LastView == minlv).Hash;
            var total = Count();
            status = $"{array[ikey].Key}:{array[ikey].Value.Count}/{total}";
        }
        */

        /*
        public static void GetNextView(out string bestHash, out string status)
        {
            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.Where(IsValid).ToArray();
            }

            var mincounter = imgArray.Min(e => e.Counter);
            imgArray = imgArray.Where(e => e.Counter == mincounter).ToArray();
            var nextmin = imgArray.Min(e => e.Next.Substring(0, 4));
            var array = imgArray.Where(e => e.Next.Substring(0, 4).Equals(nextmin)).ToArray();
            var r = AppVars.RandomNext(array.Length);
            bestHash = array[r].Hash;
            var total = Count();
            status = $"{array.Length}/{imgArray.Length}/{total}";
        }
        */

        /*
        public static void GetNextView(out string bestHash, out string status)
        {
            Img[] imgArray;
            lock (_lock) {
                imgArray = _imgList.Values.Where(IsValid).ToArray();
            }

            var mincounter = imgArray.Min(e => e.Counter);
            imgArray = imgArray.Where(e => e.Counter == mincounter).ToArray();

            bestHash = null;
            var m = "NONE";
            var array = Array.Empty<Img>();
            while (bestHash == null) {
                var rm = AppVars.RandomNext(6);
                switch (rm) {
                    case 0:
                        m = "R";
                        array = imgArray;
                        break;
                    case 1:
                        m = "L";
                        var lvmin = imgArray.Min(e => e.LastView.Date);
                        array = imgArray.Where(e => e.LastView.Date == lvmin).ToArray();
                        break;
                    case 2:
                        m = "N";
                        var nextmin = imgArray.Min(e => e.Next.Substring(0, 4));
                        array = imgArray.Where(e => e.Next.Substring(0, 4).Equals(nextmin)).ToArray();
                        break;
                    case 3:
                        m = "F";
                        array = imgArray.Where(e => e.Family > 0).ToArray();
                        break;
                    case 4:
                        m = "H";
                        array = imgArray.Where(e => e.Counter > 0).ToArray();
                        break;
                    case 5:
                        m = "+";
                        array = imgArray.Where(e => !e.Verified).ToArray();
                        break;
                }

                if (array.Length > 0) {
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
