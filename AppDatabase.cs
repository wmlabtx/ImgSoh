using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ImgSoh
{
    public static class AppDatabase
    {
        private static readonly SqlConnection _sqlConnection;
        private static readonly object _sqlLock = new object();
        private static readonly SortedList<string, Img> _imgList = new SortedList<string, Img>();

        static AppDatabase()
        {
            var connectionString =
                $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={AppConsts.FileDatabase};Connection Timeout=300";
            _sqlConnection = new SqlConnection(connectionString);
            _sqlConnection.Open();
        }

        public static void Load(IProgress<string> progress)
        {
            lock (_sqlLock) {
                _imgList.Clear();
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append($"{AppConsts.AttributeHash}, "); // 0
                sb.Append($"{AppConsts.AttributeFolder}, "); // 1
                sb.Append($"{AppConsts.AttributeVector}, "); // 2
                sb.Append($"{AppConsts.AttributeOrientation}, "); // 3
                sb.Append($"{AppConsts.AttributeLastView}, "); // 4
                sb.Append($"{AppConsts.AttributeNext}, "); // 5
                sb.Append($"{AppConsts.AttributeLastCheck}, "); // 6
                sb.Append($"{AppConsts.AttributeVerified}, "); // 7
                sb.Append($"{AppConsts.AttributeDistance}, "); // 8
                sb.Append($"{AppConsts.AttributeHistory}, "); // 9
                sb.Append($"{AppConsts.AttributeFamily} "); // 10
                sb.Append($"FROM {AppConsts.TableImages}");
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText = sb.ToString();
                    using (var reader = sqlCommand.ExecuteReader()) {
                        var dtn = DateTime.Now;
                        while (reader.Read()) {
                            var hash = reader.GetString(0);
                            var folder = reader.GetString(1);
                            var vector = Helper.ArrayToFloat((byte[])reader[2]);
                            var orientation = Helper.ByteToRotateFlipType(reader.GetByte(3));
                            var lastview = reader.GetDateTime(4);
                            var next = reader.GetString(5);
                            var lastcheck = reader.GetDateTime(6);
                            var verified = reader.GetBoolean(7);
                            var distance = reader.GetFloat(8);
                            var history = reader.GetString(9);
                            var family = reader.GetInt32(10);
                            var img = new Img(
                                hash: hash,
                                folder: folder,
                                vector: vector,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                lastcheck: lastcheck,
                                verified: verified,
                                distance: distance,
                                history: history,
                                family: family
                            );

                            _imgList.Add(hash, img);
                            if (!(DateTime.Now.Subtract(dtn).TotalMilliseconds > AppConsts.TimeLapse)) {
                                continue;
                            }

                            dtn = DateTime.Now;
                            var count = _imgList.Count;
                            progress?.Report($"Loading images ({count}){AppConsts.CharEllipsis}");
                        }
                    }
                }
            }
        }

        private static void ImgUpdateProperty(string hash, string key, object val)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {key} = @{key} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{key}", val);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public static void ImgDelete(string hash)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"DELETE FROM {AppConsts.TableImages} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.Clear();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.ExecuteNonQuery();
                }

                _imgList.Remove(hash);
            }
        }

        public static int ImgCount(bool newimages)
        {
            int count;
            lock (_sqlLock) {
                count = newimages ? _imgList.Count(e => !e.Value.Verified) : _imgList.Count;
            }

            return count;
        }

        public static DateTime GetMinLastCheck()
        {
            DateTime lastcheck;
            lock (_sqlLock) {
                lastcheck = _imgList.Count > 0 ? _imgList.Min(e => e.Value.LastCheck).AddSeconds(-1) : DateTime.Now;
            }

            return lastcheck;
        }

        public static void AddImg(Img img)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    var sb = new StringBuilder();
                    sb.Append($"INSERT INTO {AppConsts.TableImages} (");
                    sb.Append($"{AppConsts.AttributeHash}, ");
                    sb.Append($"{AppConsts.AttributeFolder}, ");
                    sb.Append($"{AppConsts.AttributeVector}, ");
                    sb.Append($"{AppConsts.AttributeOrientation}, ");
                    sb.Append($"{AppConsts.AttributeLastView}, ");
                    sb.Append($"{AppConsts.AttributeNext}, ");
                    sb.Append($"{AppConsts.AttributeLastCheck}, ");
                    sb.Append($"{AppConsts.AttributeVerified}, ");
                    sb.Append($"{AppConsts.AttributeDistance}, ");
                    sb.Append($"{AppConsts.AttributeHistory}, ");
                    sb.Append($"{AppConsts.AttributeFamily}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash}, ");
                    sb.Append($"@{AppConsts.AttributeFolder}, ");
                    sb.Append($"@{AppConsts.AttributeVector}, ");
                    sb.Append($"@{AppConsts.AttributeOrientation}, ");
                    sb.Append($"@{AppConsts.AttributeLastView}, ");
                    sb.Append($"@{AppConsts.AttributeNext}, ");
                    sb.Append($"@{AppConsts.AttributeLastCheck}, ");
                    sb.Append($"@{AppConsts.AttributeVerified}, ");
                    sb.Append($"@{AppConsts.AttributeDistance}, ");
                    sb.Append($"@{AppConsts.AttributeHistory}, ");
                    sb.Append($"@{AppConsts.AttributeFamily}");
                    sb.Append(')');
                    sqlCommand.CommandText = sb.ToString();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", img.Hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFolder}", img.Folder);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", Helper.ArrayFromFloat(img.GetVector()));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}",
                        Helper.RotateFlipTypeToByte(img.Orientation));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", img.Next);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", img.LastCheck);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", img.Verified);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeDistance}", img.Distance);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHistory}", img.GetHistory());
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFamily}", img.Family);
                    sqlCommand.ExecuteNonQuery();
                }

                _imgList.Add(img.Hash, img);
            }
        }

        public static bool TryGetImg(string hash, out Img img)
        {
            bool result;
            img = null;
            lock (_sqlLock) {
                result = _imgList.TryGetValue(hash, out img);
            }

            return result;
        }

        private static bool IsValid(Img imgX)
        {
            lock (_sqlLock) {
                if (imgX.GetVector() == null ||
                    imgX.GetVector().Length != AppConsts.VectorLength ||
                    imgX.Next.Equals(imgX.Hash)) {
                    return false;
                }

                if (!_imgList.TryGetValue(imgX.Next, out var imgY)) {
                    return false;
                }

                if (imgX.Family > 0 && imgX.Family == imgY.Family) {
                    return false;
                }
            }

            return true;
        }

        public static string GetNextCheck()
        {
            Img bestImgX = null;
            lock (_sqlLock) {
                foreach (var imgX in _imgList.Values) {
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

        public static SortedList<string, Img> GetCandidates()
        {
            SortedList<string, Img> shadow;
            lock (_sqlLock) {
                shadow = new SortedList<string, Img>(_imgList);
            }

            return shadow;
        }

        public static void GetNextView(out string bestHash, out string status)
        {
            bestHash = null;
            status = null;
            int total;

            Img imgX = null;
            var cnts = new [] { 0, 0, 0 };
            lock (_sqlLock) {
                total = _imgList.Count;
                foreach (var img in _imgList.Values) {
                    if (!IsValid(img)) {
                        continue; 
                    }

                    if (!img.Verified) {
                        cnts[0]++;
                    }

                    if (img.Family > 0) {
                        if (imgX == null || img.LastView < imgX.LastView) {
                            imgX = img;
                        }

                        cnts[1]++;
                        continue;
                    }

                    if (img.GetHistoryArray().Length > 0) {
                        cnts[2]++;
                    }
                }

                bestHash = imgX?.Hash;
            }

            var sb = new StringBuilder();
            sb.Append($"0:{cnts[0]}/1:{cnts[1]}/2:{cnts[2]}/{total}");
            status = sb.ToString();
        }

        public static string GetHashY(string hashX)
        {
            /*
             * 0 - imgX.Next
             * 1 - in Family
             * 2 - in History
             */

            string hashY = null;
            var imgs = new Img[] { null, null, null };
            lock (_sqlLock) {
                var imgX = _imgList[hashX];
                imgs[0] = _imgList[imgX.Next];
                if (imgX.Family > 0) {
                    var fimgs = GetFamily(imgX.Family);
                    foreach (var fimg in fimgs) {
                        if (fimg.Hash.Equals(hashX)) {
                            continue;
                        }

                        if (imgs[1] == null || fimg.LastView < imgs[1].LastView) {
                            imgs[1] = fimg;
                        }
                    }
                }

                var history = imgX.GetHistoryArray();
                if (history.Length > 0) {
                    foreach (var hash in history) {
                        if (_imgList.TryGetValue(hash, out var imgY)) {
                            if (imgs[2] == null || imgY.LastView < imgs[2].LastView) {
                                imgs[2] = imgY;
                            }
                        }
                    }
                }

                while (hashY == null) {
                    var rindex = AppVars.RandomNext(100);
                    if (rindex == 99) {
                        rindex = 2;
                    }
                    else {
                        if (rindex == 98) {
                            rindex = 1;
                        }
                        else {
                            rindex = 0;
                        }
                    }

                    if (imgs[rindex] != null) {
                        hashY = imgs[rindex].Hash;
                    }
                }
            }

            return hashY;
        }

        public static void SetVector(string hash, float[] vector)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: vector,
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        distance: o.Distance,
                        history: o.GetHistory(),
                        family: o.Family
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeVector, Helper.ArrayFromFloat(n.GetVector()));
                }
            }
        }

        public static void SetOrientation(string hash, RotateFlipType rft)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: o.GetVector(),
                        orientation: rft,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        distance: o.Distance,
                        history: o.GetHistory(),
                        family: o.Family
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeOrientation, Helper.RotateFlipTypeToByte(n.Orientation));
                }
            }
        }

        public static void SetLastView(string hash)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: o.GetVector(),
                        orientation: o.Orientation,
                        lastview: DateTime.Now, 
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        distance: o.Distance,
                        history: o.GetHistory(),
                        family: o.Family
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeLastView, n.LastView);
                }
            }
        }

        public static void SetVerified(string hash)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    if (!o.Verified) {
                        var n = new Img(
                            hash: o.Hash,
                            folder: o.Folder,
                            vector: o.GetVector(),
                            orientation: o.Orientation,
                            lastview: o.LastView,
                            next: o.Next,
                            lastcheck: o.LastCheck,
                            verified: true,
                            distance: o.Distance,
                            history: o.GetHistory(),
                            family: o.Family
                        );

                        _imgList.Remove(hash);
                        _imgList.Add(hash, n);
                        ImgUpdateProperty(hash, AppConsts.AttributeVerified, n.Verified);
                    }
                }
            }
        }

        public static void SetLastCheck(string hash, DateTime lastcheck)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: o.GetVector(),
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: lastcheck,
                        verified: o.Verified,
                        distance: o.Distance,
                        history: o.GetHistory(),
                        family: o.Family
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeLastCheck, n.LastCheck);
                }
            }
        }

        public static void SetNext(string hash, string next)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: o.GetVector(),
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        distance: o.Distance,
                        history: o.GetHistory(),
                        family: o.Family
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeNext, n.Next);
                }
            }
        }

        public static void SetDistance(string hash, float distance)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: o.GetVector(),
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        distance: distance,
                        history: o.GetHistory(),
                        family: o.Family
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeDistance, n.Distance);
                }
            }
        }

        public static void AddToHistory(string hash, string hashToAdd)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    if (o.AddToHistory(hashToAdd)) {
                        var history = o.GetHistory();
                        var n = new Img(
                            hash: o.Hash,
                            folder: o.Folder,
                            vector: o.GetVector(),
                            orientation: o.Orientation,
                            lastview: o.LastView,
                            next: o.Next,
                            lastcheck: o.LastCheck,
                            verified: o.Verified,
                            distance: o.Distance,
                            history: history,
                            family: o.Family
                        );

                        _imgList.Remove(hash);
                        _imgList.Add(hash, n);
                        ImgUpdateProperty(hash, AppConsts.AttributeHistory, n.GetHistory());
                    }
                }
            }
        }

        public static void RemoveFromHistory(string hash, string hashToDelete)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    if (o.RemoveFromHistory(hashToDelete)) {
                        var history = o.GetHistory();
                        var n = new Img(
                            hash: o.Hash,
                            folder: o.Folder,
                            vector: o.GetVector(),
                            orientation: o.Orientation,
                            lastview: o.LastView,
                            next: o.Next,
                            lastcheck: o.LastCheck,
                            verified: o.Verified,
                            distance: o.Distance,
                            history: history,
                            family: o.Family
                        );

                        _imgList.Remove(hash);
                        _imgList.Add(hash, n);
                        ImgUpdateProperty(hash, AppConsts.AttributeHistory, n.GetHistory());
                    }
                }
            }
        }

        public static void SetFamily(string hash, int family)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: o.GetVector(),
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        distance: o.Distance,
                        history: o.GetHistory(),
                        family: family
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeFamily, n.Family);
                }
            }
        }

        public static void RenameFamily(int ofamily, int nfamily)
        {
            lock (_sqlLock) {
                var fo = GetFamily(ofamily);
                foreach (var imgo in fo) {
                    SetFamily(imgo.Hash, nfamily);
                }
            }
        }

        public static int GetNewFamily()
        {
            int[] families;
            lock (_sqlLock) {
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

        public static Img[] GetFamily(int family)
        {
            if (family == 0) {
                return Array.Empty<Img>();
            }

            Img[] array;
            lock (_sqlLock) {
                array = _imgList.Where(e => e.Value.Family == family).Select(e => e.Value).ToArray();
            }

            return array;
        }
    }
}