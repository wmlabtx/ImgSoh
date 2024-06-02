using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ImgSoh
{
    public static class AppDatabase
    {
        private static readonly object _lock = new object();
        private static readonly SortedList<string, Img> _imgList = new SortedList<string, Img>();

        private static byte[] GetDefaultField(string attribute)
        {
            switch (attribute) {
                case AppConsts.AttributeDeleted: return Helper.GetRawBool(false);
                case AppConsts.AttributeHash: return Helper.GetRawString(string.Empty, 12);
                case AppConsts.AttributePath: return Helper.GetRawString(string.Empty, AppConsts.MaxPath);
                case AppConsts.AttributeExt: return Helper.GetRawString(string.Empty, 4);
                case AppConsts.AttributeVector: return Helper.GetRawVector(new float[AppConsts.VectorLength]);
                case AppConsts.AttributeOrientation: return Helper.GetRawOrientation(RotateFlipType.RotateNoneFlipNone);
                case AppConsts.AttributeLastView: return Helper.GetRawDateTime(DateTime.Now);
                case AppConsts.AttributeNext: return Helper.GetRawString(string.Empty, 16);
                case AppConsts.AttributeHorizon: return Helper.GetRawString(string.Empty, 16);
                case AppConsts.AttributePrev: return Helper.GetRawString(string.Empty, 16);
                case AppConsts.AttributeLastCheck: return Helper.GetRawDateTime(DateTime.Now);
                case AppConsts.AttributeVerified: return Helper.GetRawBool(false);
                case AppConsts.AttributeCounter: return Helper.GetRawInt(0);
                case AppConsts.AttributeTaken: return Helper.GetRawDateTime(DateTime.MinValue);
                case AppConsts.AttributeMeta: return Helper.GetRawInt(0);
                default: throw new Exception("wrong attribute");
            }
        }

        private static string GetFieldFilename(string attribute)
        {
            return $"{AppConsts.PathRoot}\\{attribute}.{AppConsts.DatExtension}";
        }

        private static byte[] LoadFile(string attribute, int collectionsize)
        {
            byte[] filearray;
            var filename = GetFieldFilename(attribute);
            lock (_lock) {
                if (!File.Exists(filename)) {
                    var element = GetDefaultField(attribute);
                    var array = new byte[collectionsize * element.Length];
                    for (var i = 0; i < collectionsize; i++) {
                        Buffer.BlockCopy(element, 0, array, i * element.Length, element.Length);
                    }

                    File.WriteAllBytes(filename, array);
                    return array;
                }

                filearray = File.ReadAllBytes(filename);
                if (!attribute.Equals(AppConsts.AttributeDeleted, StringComparison.OrdinalIgnoreCase)) {
                    var element = GetDefaultField(attribute);
                    if (filearray.Length != element.Length * collectionsize) {
                        throw new Exception("wrong filearray.Length");
                    }
                }
            }

            return filearray;
        }

        private static void SetFileSize(string attribute, int collectionsize)
        {
            var filename = GetFieldFilename(attribute);
            lock (_lock) {
                var element = GetDefaultField(attribute);
                var length = collectionsize * element.Length;
                if (File.Exists(filename)) {
                    using (var file = File.Open(filename, FileMode.Open)) {
                        file.SetLength(length);
                    }
                }
            }
        }

        private static byte[] GetRawField(string attribute, int index, byte[] array)
        {
            var raw = GetDefaultField(attribute);
            var offset = raw.Length * (index - 1);
            if (offset + raw.Length <= array.Length) {
                Buffer.BlockCopy(array, offset, raw, 0, raw.Length);
                return raw;
            }

            return null;
        }

        private static void SetRawField(string attribute, int index, byte[] array)
        {
            var raw = GetDefaultField(attribute);
            var offset = raw.Length * (index - 1);
            var filename = GetFieldFilename(attribute);
            lock (_lock) {
                using (var fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                    fs.Seek(offset, SeekOrigin.Begin);
                    fs.Write(array, 0, array.Length);
                }
            }
        }

        public static void LoadImages(IProgress<string> progress)
        {
            progress?.Report($"Loading images{AppConsts.CharEllipsis}");
            lock (_lock) {
                _imgList.Clear();
                var deleted_bytearray = LoadFile(AppConsts.AttributeDeleted, 0);
                var collectionsize = deleted_bytearray.Length;
                var hash_bytearray = LoadFile(AppConsts.AttributeHash, collectionsize);
                var path_bytearray = LoadFile(AppConsts.AttributePath, collectionsize);
                var ext_bytearray = LoadFile(AppConsts.AttributeExt, collectionsize);
                var vector_bytearray = LoadFile(AppConsts.AttributeVector, collectionsize);
                var orientation_bytearray = LoadFile(AppConsts.AttributeOrientation, collectionsize);
                var lastview_bytearray = LoadFile(AppConsts.AttributeLastView, collectionsize);
                var next_bytearray = LoadFile(AppConsts.AttributeNext, collectionsize);
                var horizon_bytearray = LoadFile(AppConsts.AttributeHorizon, collectionsize);
                var prev_bytearray = LoadFile(AppConsts.AttributePrev, collectionsize);
                var lastcheck_bytearray = LoadFile(AppConsts.AttributeLastCheck, collectionsize);
                var verified_bytearray = LoadFile(AppConsts.AttributeVerified, collectionsize);
                var counter_bytearray = LoadFile(AppConsts.AttributeCounter, collectionsize);
                var taken_bytearray = LoadFile(AppConsts.AttributeTaken, collectionsize);
                var meta_bytearray = LoadFile(AppConsts.AttributeMeta, collectionsize);
                var dtn = DateTime.Now;
                for (var i = 1; i <= collectionsize; i++) {
                    var deleted = Helper.SetRawBool(GetRawField(AppConsts.AttributeDeleted, i, deleted_bytearray));
                    var hash = Helper.SetRawString(GetRawField(AppConsts.AttributeHash, i, hash_bytearray));
                    var path = Helper.SetRawString(GetRawField(AppConsts.AttributePath, i, path_bytearray));
                    var ext = Helper.SetRawString(GetRawField(AppConsts.AttributeExt, i, ext_bytearray));
                    var vector = Helper.SetRawVector(GetRawField(AppConsts.AttributeVector, i, vector_bytearray));
                    var orientation = Helper.SetRawOrientation(GetRawField(AppConsts.AttributeOrientation, i, orientation_bytearray));
                    var lastview = Helper.SetRawDateTime(GetRawField(AppConsts.AttributeLastView, i, lastview_bytearray));
                    var next = Helper.SetRawString(GetRawField(AppConsts.AttributeNext, i, next_bytearray));
                    var horizon = Helper.SetRawString(GetRawField(AppConsts.AttributeHorizon, i, horizon_bytearray));
                    var prev = Helper.SetRawString(GetRawField(AppConsts.AttributePrev, i, prev_bytearray));
                    var lastcheck = Helper.SetRawDateTime(GetRawField(AppConsts.AttributeLastCheck, i, lastcheck_bytearray));
                    var verified = Helper.SetRawBool(GetRawField(AppConsts.AttributeVerified, i, verified_bytearray));
                    var counter = Helper.SetRawInt(GetRawField(AppConsts.AttributeCounter, i, counter_bytearray));
                    var taken = Helper.SetRawDateTime(GetRawField(AppConsts.AttributeTaken, i, taken_bytearray));
                    var meta = Helper.SetRawInt(GetRawField(AppConsts.AttributeMeta, i, meta_bytearray));
                    var img = new Img(
                        index: i,
                        deleted: deleted,
                        hash: hash,
                        path: path,
                        ext: ext,
                        vector: vector,
                        orientation: orientation,
                        lastview: lastview,
                        next: next,
                        lastcheck: lastcheck,
                        verified: verified,
                        prev: prev,
                        horizon: horizon,
                        counter: counter,
                        taken: taken,
                        meta: meta
                    );

                    _imgList.Add(hash, img);
                    if (DateTime.Now.Subtract(dtn).TotalMilliseconds > AppConsts.TimeLapse) {
                        dtn = DateTime.Now;
                        var count = _imgList.Count;
                        progress?.Report($"Loading images ({count}){AppConsts.CharEllipsis}");
                    }
                }
            }
        }

        public static void Populate(IProgress<string> progress)
        {
            lock (_lock) {
                var dtn = DateTime.Now;
                var count = 0;
                foreach (var hash in _imgList.Keys) {
                    SetVerified(hash);
                    count++;
                    if (DateTime.Now.Subtract(dtn).TotalMilliseconds > AppConsts.TimeLapse) {
                        dtn = DateTime.Now;
                        progress?.Report($"Populating images ({count}){AppConsts.CharEllipsis}");
                    }
                }
            }
        }

        public static int GetAvailableIndex(string hash)
        {
            var minIndex = 0;
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    if (img.Deleted) {
                        return img.Index;
                    }

                    return 0;
                }

                foreach (var e in _imgList) {
                    if (e.Value.Deleted) {
                        return e.Value.Index;
                    }

                    if (e.Value.Index > minIndex) {
                        minIndex = e.Value.Index;
                    }
                }
            }

            return minIndex + 1;
        }

        public static void AddImg(Img img)
        {
            lock (_lock) {
                foreach (var e in _imgList) {
                    if (e.Value.Index == img.Index) {
                        _imgList.Remove(e.Key);
                        break;
                    }
                }

                _imgList.Add(img.Hash, img);

                SetRawField(AppConsts.AttributeDeleted, img.Index, Helper.GetRawBool(img.Deleted));
                SetRawField(AppConsts.AttributeHash, img.Index, Helper.GetRawString(img.Hash, 12));
                SetRawField(AppConsts.AttributePath, img.Index, Helper.GetRawString(img.Path, AppConsts.MaxPath));
                SetRawField(AppConsts.AttributeExt, img.Index, Helper.GetRawString(img.Ext, 4));
                SetRawField(AppConsts.AttributeVector, img.Index, Helper.GetRawVector(img.GetVector()));
                SetRawField(AppConsts.AttributeOrientation, img.Index, Helper.GetRawOrientation(img.Orientation));
                SetRawField(AppConsts.AttributeLastView, img.Index, Helper.GetRawDateTime(img.LastView));
                SetRawField(AppConsts.AttributeNext, img.Index, Helper.GetRawString(img.Next, 16));
                SetRawField(AppConsts.AttributeHorizon, img.Index, Helper.GetRawString(img.Horizon, 16));
                SetRawField(AppConsts.AttributePrev, img.Index, Helper.GetRawString(img.Prev, 16));
                SetRawField(AppConsts.AttributeLastCheck, img.Index, Helper.GetRawDateTime(img.LastCheck));
                SetRawField(AppConsts.AttributeVerified, img.Index, Helper.GetRawBool(img.Verified));
                SetRawField(AppConsts.AttributeCounter, img.Index, Helper.GetRawInt(img.Counter));
                SetRawField(AppConsts.AttributeTaken, img.Index, Helper.GetRawDateTime(img.Taken));
                SetRawField(AppConsts.AttributeMeta, img.Index, Helper.GetRawInt(img.Meta));
            }
        }

        public static int ImgCount()
        {
            lock (_lock) {
                return _imgList.Count(e => !e.Value.Deleted);
            }
        }

        public static bool TryGetImg(string hash, out Img img)
        {
            bool result;
            img = null;
            lock (_lock) {
                result = _imgList.TryGetValue(hash, out img);
            }

            return result;
        }

        private static bool IsValid(Img imgX)
        {
            lock (_lock) {
                if (imgX.Deleted) {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(imgX.Next) ||
                    (!string.IsNullOrWhiteSpace(imgX.Prev) &&
                     !string.IsNullOrWhiteSpace(imgX.Horizon) &&
                     string.CompareOrdinal(imgX.Prev, imgX.Horizon) > 0) ||
                    (!string.IsNullOrWhiteSpace(imgX.Horizon) && 
                     !string.IsNullOrWhiteSpace(imgX.Next) && 
                     string.CompareOrdinal(imgX.Horizon, imgX.Next) >= 0)
                    ) {
                    return false;
                }

                var next = imgX.Next.Substring(4);
                if (!TryGetImg(next, out var imgY)) {
                    return false;
                }

                if (imgY.Deleted) {
                    return false;
                }
            }

            return true;
        }

        public static DateTime GetLastView()
        {
            var lastview = DateTime.Now;
            lock (_lock) {
                if (_imgList.Count > 0) {
                    lastview = _imgList.Min(e => e.Value.LastView).AddSeconds(-1);
                }
            }

            return lastview;
        }

        public static string GetNextCheck()
        {
            Img bestImgX = null;
            lock (_lock) {
                foreach (var imgX in _imgList.Values) {
                    if (imgX.Deleted) {
                        continue;
                    }

                    if (!IsValid(imgX)) {
                        bestImgX = imgX;
                        break;
                    }

                    if (bestImgX == null || imgX.LastCheck < bestImgX.LastCheck) {
                        bestImgX = imgX;
                    }
                }
            }

            var hash = bestImgX?.Hash;
            return hash;
        }

        public static List<Img> GetCandidates(string hashX)
        {
            List<Img> shadow;
            lock (_lock) {
                shadow = _imgList.Values.Where(e => !e.Hash.Equals(hashX) && !e.Deleted).ToList();
            }

            return shadow;
        }

        public static void GetNextView(out string bestHash, out string status)
        {
            bestHash = null;
            status = null;
            var total = ImgCount();
            int deleted; 
            lock (_lock) {
                deleted = _imgList.Count(e => e.Value.Deleted);
                var valids = _imgList.Values.Where(e => !e.Deleted && IsValid(e)).ToArray();
                var notverifiedCounter = valids.Count(e => !e.Verified);
                var scopeActual = notverifiedCounter > 0 ? valids.Where(e => !e.Verified).ToArray() : valids;
                var minNext = scopeActual.Min(e => e.Next.Substring(0, 4));
                if (minNext.StartsWith("0000")) {
                    scopeActual = scopeActual.Where(e => e.Next.Substring(0, 4).Equals(minNext)).ToArray();
                }
                else {
                    if (minNext.StartsWith("000")) {
                        scopeActual = scopeActual.Where(e => e.Next.Substring(0, 3).Equals("000")).ToArray();
                    }
                    else {
                        if (minNext.StartsWith("00")) {
                            scopeActual = scopeActual.Where(e => e.Next.Substring(0, 2).Equals("00")).ToArray();
                        }
                        else {
                            if (minNext.StartsWith("0")) {
                                scopeActual = scopeActual.Where(e => e.Next.Substring(0, 1).Equals("0")).ToArray();
                            }
                        }
                    }
                }

                var minCounter = scopeActual.Min(e => e.Counter);
                var scopeCounter = scopeActual.Where(e => e.Counter == minCounter).ToArray();
                status = $"*{notverifiedCounter}/{AppConsts.CharCross}{deleted}/{minCounter}:{scopeCounter.Length}/{total}";
                var rindex = AppVars.RandomNext(scopeCounter.Length);
                bestHash = scopeCounter[rindex].Hash;

                /*
                var scopeValid = _imgList.Values.Where(e => IsValid(e) && !e.Deleted).ToArray();
                if (scopeValid.Length > 0) {
                    var notverifiedCounter = scopeValid.Count(e => !e.Verified);
                    var scopeActual =  notverifiedCounter > 0 ? scopeValid.Where(e => !e.Verified).ToArray() : scopeValid;
                    var minCounter = scopeActual.Min(e => e.Counter);
                    var scopeCounter = scopeActual.Where(e => e.Counter == minCounter).ToArray();
                    var minNext = scopeCounter.Min(e => e.Next.Substring(0, 2));
                    var scopeNext = scopeCounter.Where(e => e.Next.StartsWith(minNext)).ToArray();
                    var minlastview = scopeNext.Min(e => e.LastView);
                    bestHash = scopeNext.First(e => e.LastView == minlastview).Hash;
                    status = $"n{notverifiedCounter}/{minCounter}:{scopeCounter.Length}/{minNext}:{scopeNext.Length}/{total}";
                }
                */
            }
        }

        public static string GetHashY(string hashX)
        {
            if (!TryGetImg(hashX, out var imgX)) {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(imgX.Prev) && TryGetImg(imgX.Prev.Substring(4), out var imgP)) {
                if (AppVars.RandomNext(10) == 0 && !imgP.Deleted) {
                    return imgP.Hash;
                }

                if (!string.IsNullOrWhiteSpace(imgX.Next) && TryGetImg(imgX.Next.Substring(4), out var imgN)) {
                    return imgN.Hash;
                }

                if (!imgP.Deleted) {
                    return imgP.Hash;
                }

                return null;
            }
            else {
                if (!string.IsNullOrWhiteSpace(imgX.Next) && TryGetImg(imgX.Next.Substring(4), out var imgN)) {
                    return imgN.Hash;
                }

                return null;
            }
        }

        public static void SetDeleted(string hash)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Deleted = true;
                    SetRawField(AppConsts.AttributeDeleted, _imgList[hash].Index, Helper.GetRawBool(true));
                }
            }
        }

        public static void SetVector(string hash, float[] vector)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].SetVector(vector);
                    SetRawField(AppConsts.AttributeVector, _imgList[hash].Index, Helper.GetRawVector(vector));
                }
            }
        }

        public static void SetOrientation(string hash, RotateFlipType orientation)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Orientation = orientation;
                    SetRawField(AppConsts.AttributeOrientation, _imgList[hash].Index, Helper.GetRawOrientation(orientation));
                }
            }
        }

        public static void SetLastView(string hash)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].LastView = DateTime.Now;
                    SetRawField(AppConsts.AttributeLastView, _imgList[hash].Index, Helper.GetRawDateTime(_imgList[hash].LastView));
                }
            }
        }

        public static void SetVerified(string hash)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Verified = true;
                    SetRawField(AppConsts.AttributeVerified, _imgList[hash].Index, Helper.GetRawBool(true));
                }
            }
        }

        public static void SetLastCheck(string hash)
        {
            var lastcheck = DateTime.Now;
            SetLastCheck(hash, lastcheck);
        }

        private static void SetLastCheck(string hash, DateTime lastcheck)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].LastCheck = lastcheck;
                    SetRawField(AppConsts.AttributeLastCheck, _imgList[hash].Index, Helper.GetRawDateTime(lastcheck));
                }
            }
        }

        public static void SetNext(string hash, string next)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Next = next;
                    SetRawField(AppConsts.AttributeNext, _imgList[hash].Index, Helper.GetRawString(next, 16));
                }
            }
        }

        public static void SetPrev(string hash, string prev)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Prev = prev;
                    SetRawField(AppConsts.AttributePrev, _imgList[hash].Index, Helper.GetRawString(prev, 16));
                }
            }
        }

        public static void SetHorizon(string hash)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Horizon = _imgList[hash].Next;
                    SetRawField(AppConsts.AttributeHorizon, _imgList[hash].Index, Helper.GetRawString(_imgList[hash].Next, 16));
                }
            }
        }

        public static void SetCounter(string hash, int counter)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Counter = counter;
                    SetRawField(AppConsts.AttributeCounter, _imgList[hash].Index, Helper.GetRawInt(counter));
                }
            }
        }

        public static void SetPath(string hash, string path)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Path = path;
                    SetRawField(AppConsts.AttributePath, _imgList[hash].Index, Helper.GetRawString(path, AppConsts.MaxPath));
                }
            }
        }

        public static void SetExt(string hash, string ext)
        {
            lock (_lock) {
                if (_imgList.ContainsKey(hash)) {
                    _imgList[hash].Ext = ext;
                    SetRawField(AppConsts.AttributeExt, _imgList[hash].Index, Helper.GetRawString(ext, 4));
                }
            }
        }

        public static void Compact(IProgress<string> progress)
        {
            int countMoved;
            do {
                countMoved = 0;
                Img imgDst = null;
                Img imgSrc = null;
                lock (_lock) {
                    foreach (var img in _imgList.Values) {
                        if (img.Deleted && imgDst == null) {
                            imgDst = img;
                        }

                        if (imgSrc == null) {
                            imgSrc = img;
                        }
                        else {
                            if (img.Index > imgSrc.Index) {
                                imgSrc = img;
                            }
                        }
                    }

                    // check if the last is deleted
                    if (imgSrc != null && imgSrc.Deleted) {
                        _imgList.Remove(imgSrc.Hash);
                        progress?.Report($"Trimmed: {imgSrc.Index}");
                        countMoved = 1;
                    }
                    else {
                        if (imgSrc != null && imgDst != null && imgDst.Index < imgSrc.Index) {
                            var imgMov = new Img(
                                index: imgDst.Index,
                                deleted: false,
                                hash: imgSrc.Hash,
                                path: imgSrc.Path,
                                ext: imgSrc.Ext,
                                vector: imgSrc.GetVector(),
                                orientation: imgSrc.Orientation,
                                lastview: imgSrc.LastView,
                                next: imgSrc.Next,
                                lastcheck: imgSrc.LastCheck,
                                verified: imgSrc.Verified,
                                prev: imgSrc.Prev,
                                horizon: imgSrc.Horizon,
                                counter: imgSrc.Counter,
                                taken: imgSrc.Taken,
                                 meta: imgSrc.Meta
                            );

                            _imgList.Remove(imgSrc.Hash);
                            _imgList.Remove(imgDst.Hash);
                            _imgList.Add(imgMov.Hash, imgMov);
                            progress?.Report($"Moved: {imgSrc.Index}{AppConsts.CharRightArrow}{imgDst.Index}");
                            countMoved = 1;
                        }
                    }

                    if (countMoved > 0) {
                        var collectionsize = _imgList.Count;
                        var filename = GetFieldFilename(AppConsts.AttributeDeleted);
                        var filesize = new FileInfo(filename).Length;
                        if (filesize > collectionsize) {
                            SetFileSize(AppConsts.AttributeDeleted, collectionsize);
                            SetFileSize(AppConsts.AttributeHash, collectionsize);
                            SetFileSize(AppConsts.AttributePath, collectionsize);
                            SetFileSize(AppConsts.AttributeExt, collectionsize);
                            SetFileSize(AppConsts.AttributeVector, collectionsize);
                            SetFileSize(AppConsts.AttributeOrientation, collectionsize);
                            SetFileSize(AppConsts.AttributeLastView, collectionsize);
                            SetFileSize(AppConsts.AttributeNext, collectionsize);
                            SetFileSize(AppConsts.AttributeHorizon, collectionsize);
                            SetFileSize(AppConsts.AttributePrev, collectionsize);
                            SetFileSize(AppConsts.AttributeLastCheck, collectionsize);
                            SetFileSize(AppConsts.AttributeVerified, collectionsize);
                            SetFileSize(AppConsts.AttributeCounter, collectionsize);
                            SetFileSize(AppConsts.AttributeTaken, collectionsize);
                            SetFileSize(AppConsts.AttributeMeta, collectionsize);
                        }
                    }
                }
            } while (countMoved > 0);
        }
    }
}