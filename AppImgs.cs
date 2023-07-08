using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImgSoh
{
    public static class AppImgs
    {
        private static readonly SortedList<string, Img> _imgList = new SortedList<string, Img>();
        private static readonly object _imgLock = new object();
        private static long _lastmonth = -1;

        public static void Clear()
        {
            _imgList.Clear();
        }

        public static int Count()
        {
            int count;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    count = _imgList.Count;
                }
                finally { 
                    Monitor.Exit(_imgLock); 
                }
            }
            else {
                throw new Exception();
            }

            return count;
        }

        public static void Add(Img img)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    _imgList.Add(img.Hash, img);
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static bool TryGetValue(string hash, out Img img)
        {
            bool result;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    result = _imgList.TryGetValue(hash, out Img _img);
                    img = _img;
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            return result;
        }

        public static bool ContainsHash(string hash)
        {
            bool result;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    result = _imgList.ContainsKey(hash);
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            return result;
        }

        public static void Delete(Img img)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    _imgList.Remove(img.Hash);
                    /*
                    var lc = _imgList.Min(e => e.Value.LastCheck);
                    foreach (var e in _imgList.Where(e => e.Value.Next.Equals(img.Hash))) { 
                        e.Value.SetLastCheck(lc);
                    }
                    */
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        /*
        public static DateTime GetMinLastView()
        {
            DateTime lv;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    lv = _imgList.Count > 0 ? _imgList.Min(e => e.Value.LastView).AddSeconds(-1) : DateTime.Now;
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            return lv;
        }
        */

        public static Img GetNextView()
        {
            Img imgX = null;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    if (_imgList.Count > 2) {
                        var minticks = long.MaxValue;
                        var minrev = short.MinValue;
                        foreach (var img in _imgList.Values) {
                            if (img.Next.Equals(img.Hash) || !_imgList.TryGetValue(img.Next, out var imgnext)) {
                                continue;
                            }

                            var ticks = Math.Min(img.LastView.Ticks, imgnext.LastView.Ticks);
                            var rev = Math.Min(img.Review, imgnext.Review);
                            var days = (long)TimeSpan.FromTicks(ticks).TotalDays;
                            if (days == _lastmonth) {
                                continue;
                            }

                            if (imgX == null) {
                                imgX = img;
                                minticks = ticks;
                                minrev = rev;
                            }
                            else {
                                if (rev < minrev) {
                                    imgX = img;
                                    minticks = ticks;
                                    minrev = rev;
                                }
                                else {
                                    if (rev == minrev && img.Distance < imgX.Distance) {
                                        imgX = img;
                                        minticks = ticks;
                                        minrev = rev;
                                    }
                                }
                            }
                        }

                        _lastmonth = (long)TimeSpan.FromTicks(minticks).TotalDays;
                    }
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            return imgX;
        }

        public static Img GetNextCheck()
        {
            Img imgX = null;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    foreach (var img in _imgList.Values) {
                        if (img.Next.Equals(img.Hash) || !_imgList.ContainsKey(img.Next) || img.GetVector() == null || img.GetVector().Length != 4096) {
                            imgX = img;
                            break;
                        }

                        if (imgX == null || img.LastCheck < imgX.LastCheck) {
                            imgX = img;
                        }
                    }
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            return imgX;
        }

        public static string[] GetKeys()
        {
            string[] result;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    result = _imgList.Keys.ToArray();
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            return result;
        }

        public static SortedList<string, Img> GetShadow()
        {
            SortedList<string, Img> shadow;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    shadow = new SortedList<string, Img>(_imgList);
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            return shadow;
        }

        public static string GetFolder()
        {
            var iFolder = AppVars.RandomNext(256);
            var folder = $"{iFolder:x2}";
            return folder;
        }

        public static List<string> GetSimilars(Img imgX)
        {
            var similars = new List<string> {
                imgX.Next
            };

            /*
            var shadow = GetShadow();
            shadow.Remove(imgX.Hash);
            var vlist = new List<Tuple<string, float>>();
            var dlist = new List<Tuple<string, float>>();
            foreach (var e in shadow) {
                var vd = VggHelper.GetDistance(imgX.GetVector(), e.Value.GetVector());
                vlist.Add(Tuple.Create(e.Key, vd));
                var dd = (float)Math.Abs(imgX.DateTaken.Subtract(e.Value.DateTaken).TotalDays);
                dlist.Add(Tuple.Create(e.Key, dd));
            }

            var vl = vlist.OrderBy(e => e.Item2).ToArray();
            var dl = dlist.OrderBy(e => e.Item2).ToArray();

            var i = 0;
            while (i < vl.Length && similars.Count < 100) {
                if (!similars.Any(e => e == vl[i].Item1)) {
                    similars.Add(vl[i].Item1);
                }

                if (!similars.Any(e => e == dl[i].Item1)) {
                    similars.Add(dl[i].Item1);
                }

                i++;
            }
            */

            return similars;
        }

        public static void Populate(IProgress<string> progress)
        {
            var shadow = GetShadow();
            var now = DateTime.Now;
            var counter = 0;
            foreach (var e in shadow) {
                counter++;
                var rImg = e.Value;
                if (rImg.LastView.Year <= 2020) {
                    rImg.SetReview(0);
                    var shortFilename = rImg.GetShortFileName();
                    var message = $"{counter}) {shortFilename}: {rImg.Distance:F4} ({rImg.Review})";
                    progress.Report(message);
                }
                else {
                    var days = now.Subtract(rImg.LastView).Days;
                    if (days < 3) {
                        rImg.SetReview(2);
                        var shortFilename = rImg.GetShortFileName();
                        var message = $"{counter}) {shortFilename}: {rImg.Distance:F4} ({rImg.Review})";
                        progress.Report(message);
                    }
                }
            }
        }

        public static string GetCounters()
        {
            int total;
            int revCount;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    total = _imgList.Count;
                    var minRev = _imgList.Values.Min(e => e.Review);
                    revCount = _imgList.Values.Count(e => e.Review == minRev);
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            var result = $"{revCount}/{total}";
            return result;
        }

        public static void IncrementReview(string hash)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    if (_imgList.TryGetValue(hash, out var img)) {
                        img.IncrementReview();
                    }
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void SetNext(string hash, string next)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    if (_imgList.TryGetValue(hash, out var img)) {
                        img.SetNext(next);
                    }
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void SetLastCheck(string hash, DateTime lastcheck)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    if (_imgList.TryGetValue(hash, out var img)) {
                        img.SetLastCheck(lastcheck);
                    }
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void SetLastView(string hash, DateTime lastview)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    if (_imgList.TryGetValue(hash, out var img)) {
                        img.SetLastView(lastview);
                    }
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void SetDistance(string hash, float distance)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    if (_imgList.TryGetValue(hash, out var img)) {
                        img.SetDistance(distance);
                    }
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void SetVector(string hash, byte[] vector)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    if (_imgList.TryGetValue(hash, out var img)) {
                        img.SetVector(vector);
                    }
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }
    }
}
