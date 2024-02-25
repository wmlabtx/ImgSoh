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
                sb.Append($"{AppConsts.AttributeHistory}, "); // 8
                sb.Append($"{AppConsts.AttributeFingerPrint} "); // 9
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
                            var history = reader.GetString(8);
                            var fingerprint = reader.GetString(9);
                            var img = new Img(
                                hash: hash,
                                folder: folder,
                                vector: vector,
                                fingerprint: fingerprint,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                lastcheck: lastcheck,
                                verified: verified,
                                history: history
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
                    sb.Append($"{AppConsts.AttributeHistory}, ");
                    sb.Append($"{AppConsts.AttributeFingerPrint}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash}, ");
                    sb.Append($"@{AppConsts.AttributeFolder}, ");
                    sb.Append($"@{AppConsts.AttributeVector}, ");
                    sb.Append($"@{AppConsts.AttributeOrientation}, ");
                    sb.Append($"@{AppConsts.AttributeLastView}, ");
                    sb.Append($"@{AppConsts.AttributeNext}, ");
                    sb.Append($"@{AppConsts.AttributeLastCheck}, ");
                    sb.Append($"@{AppConsts.AttributeVerified}, ");
                    sb.Append($"@{AppConsts.AttributeHistory}, ");
                    sb.Append($"@{AppConsts.AttributeFingerPrint}");
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
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFingerPrint}", img.FingerPrint);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHistory}", img.History);
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
                    imgX.FingerPrint.Length == 0 ||
                    imgX.Next.Equals(imgX.Hash) ||
                    !_imgList.ContainsKey(imgX.Next)) {
                    return false;
                }

                foreach (var hash in imgX.HistoryArray) {
                    if (!_imgList.ContainsKey(hash)) {
                        return false;
                    }
                }
            }

            return true;
        }

        public static string GetNextCheck()
        {
            string hash;
            lock (_sqlLock) {
                hash = _imgList
                    .OrderBy(e => e.Value.LastCheck)
                    .FirstOrDefault()
                    .Key;
                /*
                var scope = _imgList
                    .Where(e => !IsValid(e.Value))
                    .ToList();

                if (scope.Count == 0) {
                    scope = _imgList
                        .OrderBy(e => e.Value.HistoryCount)
                        .ThenBy(e => e.Value.LastCheck)
                        .Take(1)
                        .ToList();
                }

                hash = scope
                    .FirstOrDefault()
                    .Key;
                */
            }

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
            var bads = 0;
            var news = 0;
            var h = int.MaxValue;
            var hc = 0;

            int total;
            lock (_sqlLock) {
                total = _imgList.Count;
                foreach (var imgX in _imgList.Values) {
                    if (!IsValid(imgX)) {
                        bads++;
                    }
                    else {
                        if (!imgX.Verified) {
                            news++;
                        }
                        else {
                            if (imgX.HistoryCount < h) {
                                h = imgX.HistoryCount;
                                hc = 1;
                            }
                            else {
                                if (imgX.HistoryCount == h) {
                                    hc++;
                                }
                            }
                        }
                    }
                }

                var daysX = -1;
                var daysY = -1;
                foreach (var imgX in _imgList.Values) {
                    if (!_imgList.TryGetValue(imgX.Next, out var imgY)) {
                        continue;
                    }

                    if (!imgX.Verified) {
                        bestHash = imgX.Hash;
                        break;
                    }

                    var dX = (int)DateTime.Now.Subtract(imgX.LastView).TotalDays;
                    var dY = (int)DateTime.Now.Subtract(imgY.LastView).TotalDays;
                    if (dX > daysX || (dX == daysX && dY > daysY)) {
                        daysX = dX;
                        daysY = dY;
                        bestHash = imgX.Hash;
                    }
                }
            }

            var sb = new StringBuilder();
            if (news > 0) {
                if (sb.Length > 0) {
                    sb.Append('/');
                }

                sb.Append($"n{news}");
            }

            if (h < int.MaxValue) {
                if (sb.Length > 0) {
                    sb.Append('/');
                }

                sb.Append($"{h}:{hc}");
            }

            if (bads > 0) {
                if (sb.Length > 0) {
                    sb.Append('/');
                }

                sb.Append($"b{bads}");
            }

            if (sb.Length > 0) {
                sb.Append('/');
            }

            sb.Append($"{total}");
            status = sb.ToString();
        }

        public static void SetVector(string hash, float[] vector)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: vector,
                        fingerprint: o.FingerPrint,
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        history: o.History
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
                        fingerprint: o.FingerPrint,
                        orientation: rft,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        history: o.History
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
                        fingerprint: o.FingerPrint,
                        orientation: o.Orientation,
                        lastview: DateTime.Now, 
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        history: o.History
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
                            fingerprint: o.FingerPrint,
                            orientation: o.Orientation,
                            lastview: o.LastView,
                            next: o.Next,
                            lastcheck: o.LastCheck,
                            verified: true,
                            history: o.History
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
                        fingerprint: o.FingerPrint,
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: lastcheck,
                        verified: o.Verified,
                        history: o.History
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
                        fingerprint: o.FingerPrint,
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        history: o.History
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeNext, n.Next);
                }
            }
        }

        public static void AddToHistory(string hash, string next)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var historysortedset = Helper.StringToSortedSet(o.History);
                    if (historysortedset.Add(next)) {
                        var n = new Img(
                            hash: o.Hash,
                            folder: o.Folder,
                            vector: o.GetVector(),
                            fingerprint: o.FingerPrint,
                            orientation: o.Orientation,
                            lastview: o.LastView,
                            next: o.Next,
                            lastcheck: o.LastCheck,
                            verified: o.Verified,
                            history: Helper.SortedSetToString(historysortedset)
                        );

                        _imgList.Remove(hash);
                        _imgList.Add(hash, n);
                        ImgUpdateProperty(hash, AppConsts.AttributeHistory, n.History);
                    }
                }
            }
        }

        public static void RemoveFromHistory(string hash, string next)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var historysortedset = Helper.StringToSortedSet(o.History);
                    if (historysortedset.Remove(next)) {
                        var n = new Img(
                            hash: o.Hash,
                            folder: o.Folder,
                            vector: o.GetVector(),
                            fingerprint: o.FingerPrint,
                            orientation: o.Orientation,
                            lastview: o.LastView,
                            next: o.Next,
                            lastcheck: o.LastCheck,
                            verified: o.Verified,
                            history: Helper.SortedSetToString(historysortedset)
                        );

                        _imgList.Remove(hash);
                        _imgList.Add(hash, n);
                        ImgUpdateProperty(hash, AppConsts.AttributeHistory, n.History);
                    }
                }
            }
        }

        public static void SetFingerPrint(string hash, string fingerprint)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    var n = new Img(
                        hash: o.Hash,
                        folder: o.Folder,
                        vector: o.GetVector(),
                        fingerprint: fingerprint,
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        history: o.History
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeFingerPrint, n.FingerPrint);
                }
            }
        }
    }
}