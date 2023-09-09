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
        private static readonly List<Tuple<string, string>> _pairList = new List<Tuple<string, string>>();

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
                sb.Append($"{AppConsts.AttributeDistance}, "); // 6
                sb.Append($"{AppConsts.AttributeLastCheck}, "); // 7
                sb.Append($"{AppConsts.AttributeVerified} "); // 8
                sb.Append($"FROM {AppConsts.TableImages}");
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText = sb.ToString();
                    using (var reader = sqlCommand.ExecuteReader()) {
                        var dtn = DateTime.Now;
                        while (reader.Read()) {
                            var hash = reader.GetString(0);
                            var folder = reader.GetString(1);
                            var vector = (byte[])reader[2];
                            var orientation = Helper.ByteToRotateFlipType(reader.GetByte(3));
                            var lastview = reader.GetDateTime(4);
                            var next = reader.GetString(5);
                            var distance = reader.GetFloat(6);
                            var lastcheck = reader.GetDateTime(7);
                            var verified = reader.GetBoolean(8);
                            var img = new Img(
                                hash: hash,
                                folder: folder,
                                vector: vector,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                distance: distance,
                                lastcheck: lastcheck,
                                verified: verified
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

                sb.Clear();
                sb.Append("SELECT ");
                sb.Append($"{AppConsts.AttributeId1}, "); // 0
                sb.Append($"{AppConsts.AttributeId2} "); // 1
                sb.Append($"FROM {AppConsts.TablePairs}");
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText = sb.ToString();
                    using (var reader = sqlCommand.ExecuteReader()) {
                        var dtn = DateTime.Now;
                        while (reader.Read()) {
                            var id1 = reader.GetString(0);
                            var id2 = reader.GetString(1);
                            _pairList.Add(Tuple.Create(id1, id2));
                            if (!(DateTime.Now.Subtract(dtn).TotalMilliseconds > AppConsts.TimeLapse)) {
                                continue;
                            }

                            dtn = DateTime.Now;
                            var count = _pairList.Count;
                            progress?.Report($"Loading pairs ({count}){AppConsts.CharEllipsis}");
                        }
                    }
                }
            }
        }

        public static void ImgUpdateLastView(string hash, DateTime lastView)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {AppConsts.AttributeLastView} = @{AppConsts.AttributeLastView} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", lastView);
                    sqlCommand.ExecuteNonQuery();
                }

                var img = _imgList[hash];
                var imgnew = new Img(
                    hash: img.Hash,
                    folder: img.Folder,
                    vector: img.GetVector(),
                    orientation: img.Orientation,
                    lastview: lastView,
                    next: img.Next,
                    distance: img.Distance,
                    lastcheck: img.LastCheck,
                    verified: img.Verified
                );

                _imgList[img.Hash] = imgnew;
            }
        }

        public static void ImgUpdateOrientation(string hash, RotateFlipType rft)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {AppConsts.AttributeOrientation} = @{AppConsts.AttributeOrientation} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}",
                        Helper.RotateFlipTypeToByte(rft));
                    sqlCommand.ExecuteNonQuery();
                }

                var img = _imgList[hash];
                var imgnew = new Img(
                    hash: img.Hash,
                    folder: img.Folder,
                    vector: img.GetVector(),
                    orientation: rft,
                    lastview: img.LastView,
                    next: img.Next,
                    distance: img.Distance,
                    lastcheck: img.LastCheck,
                    verified: img.Verified
                );

                _imgList[img.Hash] = imgnew;
            }
        }

        public static Img ImgUpdateVector(string hash, byte[] vector)
        {
            Img imgnew;
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {AppConsts.AttributeVector} = @{AppConsts.AttributeVector} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", vector);
                    sqlCommand.ExecuteNonQuery();
                }

                var img = _imgList[hash];
                imgnew = new Img(
                    hash: img.Hash,
                    folder: img.Folder,
                    vector: vector,
                    orientation: img.Orientation,
                    lastview: img.LastView,
                    next: img.Next,
                    distance: img.Distance,
                    lastcheck: img.LastCheck,
                    verified: img.Verified
                );

                _imgList[img.Hash] = imgnew;
            }

            return imgnew;
        }

        public static void ImgUpdateNext(string hash, string next)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {AppConsts.AttributeNext} = @{AppConsts.AttributeNext} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", next);
                    sqlCommand.ExecuteNonQuery();
                }

                if (_imgList.TryGetValue(hash, out var img)) {
                    var imgnew = new Img(
                        hash: img.Hash,
                        folder: img.Folder,
                        vector: img.GetVector(),
                        orientation: img.Orientation,
                        lastview: img.LastView,
                        next: next,
                        distance: img.Distance,
                        lastcheck: img.LastCheck,
                        verified: img.Verified
                    );

                    _imgList[img.Hash] = imgnew;
                }
            }
        }

        public static void ImgUpdateDistance(string hash, float distance)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {AppConsts.AttributeDistance} = @{AppConsts.AttributeDistance} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeDistance}", distance);
                    sqlCommand.ExecuteNonQuery();
                }

                if (_imgList.TryGetValue(hash, out var img)) {
                    var imgnew = new Img(
                        hash: img.Hash,
                        folder: img.Folder,
                        vector: img.GetVector(),
                        orientation: img.Orientation,
                        lastview: img.LastView,
                        next: img.Next,
                        distance: distance,
                        lastcheck: img.LastCheck,
                        verified: img.Verified
                    );

                    _imgList[img.Hash] = imgnew;
                }
            }
        }

        public static void ImgUpdateLastCheck(string hash, DateTime lastCheck)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {AppConsts.AttributeLastCheck} = @{AppConsts.AttributeLastCheck} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", lastCheck);
                    sqlCommand.ExecuteNonQuery();
                }

                if (_imgList.TryGetValue(hash, out var img)) {
                    var imgnew = new Img(
                        hash: img.Hash,
                        folder: img.Folder,
                        vector: img.GetVector(),
                        orientation: img.Orientation,
                        lastview: img.LastView,
                        next: img.Next,
                        distance: img.Distance,
                        lastcheck: lastCheck,
                        verified: img.Verified
                    );

                    _imgList[img.Hash] = imgnew;
                }
            }
        }

        public static void ImgUpdateVerified(string hash)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {AppConsts.AttributeVerified} = @{AppConsts.AttributeVerified} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", true);
                    sqlCommand.ExecuteNonQuery();
                }

                var img = _imgList[hash];
                var imgnew = new Img(
                    hash: img.Hash,
                    folder: img.Folder,
                    vector: img.GetVector(),
                    orientation: img.Orientation,
                    lastview: img.LastView,
                    next: img.Next,
                    distance: img.Distance,
                    lastcheck: img.LastCheck,
                    verified: true
                );

                _imgList[img.Hash] = imgnew;
            }
        }

        public static void DeleteImg(string hash)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    var transaction = _sqlConnection.BeginTransaction();
                    try {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText =
                            $"DELETE FROM {AppConsts.TableImages} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                        sqlCommand.Parameters.Clear();
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        sqlCommand.Transaction = transaction;
                        sqlCommand.ExecuteNonQuery();

                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText =
                            $"DELETE FROM {AppConsts.TablePairs} WHERE {AppConsts.AttributeId1} = @{hash} OR {AppConsts.AttributeId2} = @{hash}";
                        sqlCommand.Parameters.Clear();
                        sqlCommand.Parameters.AddWithValue($"@{hash}", hash);
                        sqlCommand.Transaction = transaction;
                        sqlCommand.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch {
                        try {
                            transaction.Rollback();
                        }
                        catch {
                            // ignored
                        }
                    }
                }

                _imgList.Remove(hash);
                _pairList.RemoveAll(e => e.Item1.Equals(hash) || e.Item2.Equals(hash));
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

        public static int PairCount()
        {
            int count;
            lock (_sqlLock) {
                count = _pairList.Count;
            }

            return count;
        }

        public static bool AddPair(string id1, string id2)
        {
            bool result;
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    var transaction = _sqlConnection.BeginTransaction();
                    try {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText =
                            $"DELETE FROM {AppConsts.TablePairs} WHERE ({AppConsts.AttributeId1} = @{id1} AND {AppConsts.AttributeId2} = @{id2}) OR ({AppConsts.AttributeId1} = @{id2} AND {AppConsts.AttributeId2} = @{id1})";
                        sqlCommand.Parameters.Clear();
                        sqlCommand.Parameters.AddWithValue($"@{id1}", id1);
                        sqlCommand.Parameters.AddWithValue($"@{id2}", id2);
                        sqlCommand.Transaction = transaction;
                        sqlCommand.ExecuteNonQuery();

                        sqlCommand.Connection = _sqlConnection;
                        var sb = new StringBuilder();
                        sb.Append($"INSERT INTO {AppConsts.TablePairs} (");
                        sb.Append($"{AppConsts.AttributeId1}, ");
                        sb.Append($"{AppConsts.AttributeId2}");
                        sb.Append(") VALUES (");
                        sb.Append($"@{AppConsts.AttributeId1}, ");
                        sb.Append($"@{AppConsts.AttributeId2}");
                        sb.Append(')');
                        sqlCommand.CommandText = sb.ToString();
                        sqlCommand.Parameters.Clear();
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeId1}", id1);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeId2}", id2);
                        sqlCommand.Transaction = transaction;
                        sqlCommand.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch {
                        try {
                            transaction.Rollback();
                        }
                        catch {
                            // ignored
                        }
                    }

                    var count = _pairList.Count(e =>
                        (e.Item1.Equals(id1) && e.Item2.Equals(id2)) || (e.Item1.Equals(id2) && e.Item2.Equals(id1)));
                    if (count > 0) {
                        _pairList.RemoveAll(e =>
                            (e.Item1.Equals(id1) && e.Item2.Equals(id2)) ||
                            (e.Item1.Equals(id2) && e.Item2.Equals(id1)));
                    }

                    _pairList.Add(Tuple.Create(id1, id2));
                    result = count == 0;
                }
            }

            return result;
        }

        /*
        private static void DeletePair(string id)
        {
            lock (_sqlLock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"DELETE FROM {AppConsts.TablePairs} WHERE ({AppConsts.AttributeId1} = @{id} OR {AppConsts.AttributeId2} = @{id})";
                    sqlCommand.Parameters.Clear();
                    sqlCommand.Parameters.AddWithValue($"@{id}", id);
                    sqlCommand.ExecuteNonQuery();
                }

                _pairList.RemoveAll(e => e.Item1.Equals(id) || e.Item2.Equals(id));
            }
        }
        */

        public static SortedList<string, string> GetPairs(string hash)
        {
            var result = new SortedList<string, string>();
            lock (_sqlLock) {
                foreach (var e in _pairList) {
                    if (e.Item1.Equals(hash) && !result.ContainsKey(e.Item2)) {
                        result.Add(e.Item2, e.Item1);
                    }

                    if (e.Item2.Equals(hash) && !result.ContainsKey(e.Item1)) {
                        result.Add(e.Item1, e.Item2);
                    }
                }
            }

            return result;
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
                    sb.Append($"{AppConsts.AttributeDistance}, ");
                    sb.Append($"{AppConsts.AttributeLastCheck}, ");
                    sb.Append($"{AppConsts.AttributeVerified}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash}, ");
                    sb.Append($"@{AppConsts.AttributeFolder}, ");
                    sb.Append($"@{AppConsts.AttributeVector}, ");
                    sb.Append($"@{AppConsts.AttributeOrientation}, ");
                    sb.Append($"@{AppConsts.AttributeLastView}, ");
                    sb.Append($"@{AppConsts.AttributeNext}, ");
                    sb.Append($"@{AppConsts.AttributeDistance}, ");
                    sb.Append($"@{AppConsts.AttributeLastCheck}, ");
                    sb.Append($"@{AppConsts.AttributeVerified}");
                    sb.Append(')');
                    sqlCommand.CommandText = sb.ToString();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", img.Hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFolder}", img.Folder);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", img.GetVector());
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}",
                        Helper.RotateFlipTypeToByte(img.Orientation));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", img.Next);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeDistance}", img.Distance);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", img.LastCheck);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", img.Verified);

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

        public static Img GetNextCheck()
        {
            Img imgCheck = null;
            lock (_sqlLock) {
                foreach (var img in _imgList.Values) {
                    if (img.GetVector() == null || img.GetVector().Length != 4096 || img.Next.Equals(img.Hash) ||
                        !_imgList.ContainsKey(img.Next)) {
                        imgCheck = img;
                        break;
                    }

                    if (imgCheck == null || img.LastCheck < imgCheck.LastCheck) {
                        imgCheck = img;
                    }
                }
            }

            return imgCheck;
        }

        public static SortedList<string, byte[]> GetVectors()
        {
            var shadow = new SortedList<string, byte[]>();
            lock (_sqlLock) {
                foreach (var img in _imgList.Values) {
                    shadow.Add(img.Hash, img.GetVector());
                }
            }

            return shadow;
        }

        public static string GetNextView()
        {
            var minsumlv = new[] { long.MaxValue, long.MaxValue, long.MaxValue };
            var hashes = new string[] { null, null, null };
            int mode;
            lock (_sqlLock) {
                var virgings = new SortedList<string, Img>(_imgList);
                foreach (var pair in _pairList) {
                    virgings.Remove(pair.Item1);
                    virgings.Remove(pair.Item2);
                }

                foreach (var imgX in _imgList.Values) {
                    if (imgX.Next.Equals(imgX.Hash) || imgX.GetVector() == null || imgX.GetVector().Length != 4096) {
                        continue;
                    }

                    if (!_imgList.TryGetValue(imgX.Next, out var imgY)) {
                        continue;
                    }

                    var sumlv = Math.Max(imgX.LastView.Ticks, imgY.LastView.Ticks);
                    var m = 0;
                    if (imgX.Verified && imgY.Verified) {
                        m = 1;
                        if (virgings.ContainsKey(imgX.Hash) || virgings.ContainsKey(imgY.Hash)) {
                            m = 2;
                        }
                    }

                    if (sumlv < minsumlv[m]) {
                        hashes[m] = imgX.Hash;
                        minsumlv[m] = sumlv;
                    }
                }

                if (hashes[0] == null && hashes[1] == null && hashes[2] == null) {
                    return null;
                }

                do {
                    mode = AppVars.RandomNext(20);
                    if (mode < 7) {
                        mode = 0;
                    }
                    else {
                        if (mode < 9) {
                            mode = 1;
                        }
                        else {
                            mode = 2;
                        }
                    }

                } while (hashes[mode] == null);
            }

            var hashView = hashes[mode];
            return hashView;
        }

        public static string GetPreviousNext(string hashX)
        {
            Img imgY = null;
            var pairs = GetPairs(hashX);
            lock (_sqlLock) {
                foreach (var p in pairs) {
                    if (_imgList.TryGetValue(p.Key, out var img)) {
                        if (imgY == null || img.LastView < imgY.LastView) {
                            imgY = img;
                        }
                    }
                }
            }

            return imgY?.Hash;
        }

        public static void Populate(IProgress<string> progress)
        {
            lock (_sqlLock) {
                var hashes = _imgList.Keys.ToArray();
                var dtn = DateTime.Now;
                var count = 0;
                foreach (var hash in hashes) {
                    var index = AppVars.RandomNext(hashes.Length);
                    var next = hashes[index];
                    ImgUpdateNext(hash, next);

                    count++;
                    if (!(DateTime.Now.Subtract(dtn).TotalMilliseconds > AppConsts.TimeLapse)) {
                        continue;
                    }

                    dtn = DateTime.Now;
                    progress?.Report($"Populating images ({count}){AppConsts.CharEllipsis}");
                }
            }
        }

        public static void Confirm(string hash)
        {
            lock (_sqlLock) {
                var vectorX = _imgList[hash].GetVector();
                var distances = new List<Tuple<string, float>>();
                foreach (var img in _imgList.Values) {
                    var distance = VggHelper.GetDistance(vectorX, img.GetVector());
                    distances.Add(Tuple.Create(img.Hash, distance));
                }

                var vicinity = distances.OrderBy(e => e.Item2).Take(50).Select(e => e.Item1).ToArray();
                var random = AppVars.RandomNext(60);
                var shift = DateTime.Now.AddSeconds(-random);
                foreach (var e in vicinity) {
                    ImgUpdateLastView(e, shift);
                    shift = shift.AddSeconds(-1);
                }
            }
        }
    }
}