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
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static Img GetNextView()
        {
            Img imgX = null;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try
                {
                    if (_imgList.Count > 2) {
                        var minlv = DateTime.MaxValue.Ticks;
                        foreach (var img in _imgList.Values) {
                            if (img.Next.Equals(img.Hash) || !_imgList.TryGetValue(img.Next, out var imgnext)) {
                                continue;
                            }

                            var lv = Math.Min(img.LastView.Ticks, imgnext.LastView.Ticks);
                            if (imgX == null || lv < minlv || (lv == minlv && img.Distance < imgX.Distance)) {
                                imgX = img;
                                minlv = lv;
                            }
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

        public static DateTime GetMinLastCheck()
        {
            DateTime lv;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    lv = _imgList.Count > 0 ? _imgList.Min(e => e.Value.LastCheck).AddSeconds(-1) : DateTime.Now;
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
                    }

                    if (imgX == null) {
                        imgX = _imgList
                            .OrderBy(e => e.Value.LastView)
                            .Take(1000)
                            .OrderBy(e => e.Value.LastCheck)
                            .First().Value;
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

        /*
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
        */

        public static string GetCounters()
        {
            short minrev;
            int total;
            int revCount;
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    total = _imgList.Count;
                    minrev = _imgList.Values.Min(e => e.Review); 
                    revCount = _imgList.Values.Count(e => e.Review == minrev);
                }
                finally {
                    Monitor.Exit(_imgLock);
                }
            }
            else {
                throw new Exception();
            }

            var result = $"{minrev}:{revCount}/{total}";
            return result;
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

        public static void SetReview(string hash, short review)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    if (_imgList.TryGetValue(hash, out var img)) {
                        img.SetReview(review);
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

        public static void VerifyPairs(string hash)
        {
            if (Monitor.TryEnter(_imgLock, AppConsts.LockTimeout)) {
                try {
                    var pairs = AppDatabase.GetPairs(hash);
                    foreach (var e in pairs) {
                        if (!_imgList.ContainsKey(e.Key)) {
                            AppDatabase.DeletePair(e.Key);
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
        }
    }
}
