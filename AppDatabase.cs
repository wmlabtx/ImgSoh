using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Data.SqlClient;
using System.Text;

namespace ImgSoh
{
    public static class AppDatabase
    {
        private static readonly object _lock = new object();
        private static readonly SqlConnection _sqlConnection;
        private static readonly SortedList<string, Img> _imgList = new SortedList<string, Img>();
        private static int _scope = 100;

        static AppDatabase()
        {
            var connectionString =
                $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={AppConsts.FileDatabase};Connection Timeout=300";
            _sqlConnection = new SqlConnection(connectionString);
            _sqlConnection.Open();
        }

        public static void LoadImages(IProgress<string> progress)
        {
            lock (_lock) {
                _imgList.Clear();
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append($"{AppConsts.AttributeHash},"); // 0
                sb.Append($"{AppConsts.AttributePath},"); // 1
                sb.Append($"{AppConsts.AttributeExt},"); // 2
                sb.Append($"{AppConsts.AttributeVector},"); // 3
                sb.Append($"{AppConsts.AttributeOrientation},"); // 4
                sb.Append($"{AppConsts.AttributeLastView},"); // 5
                sb.Append($"{AppConsts.AttributeNext},"); // 6
                sb.Append($"{AppConsts.AttributeHorizon},"); // 7
                sb.Append($"{AppConsts.AttributePrev},"); // 8
                sb.Append($"{AppConsts.AttributeLastCheck},"); // 9
                sb.Append($"{AppConsts.AttributeVerified},"); // 10
                sb.Append($"{AppConsts.AttributeCounter},"); // 11
                sb.Append($"{AppConsts.AttributeTaken},"); // 12
                sb.Append($"{AppConsts.AttributeMeta}"); // 13
                sb.Append($" FROM {AppConsts.TableImages}");
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText = sb.ToString();
                    using (var reader = sqlCommand.ExecuteReader()) {
                        var dtn = DateTime.Now;
                        while (reader.Read()) {
                            var hash = reader.GetString(0);
                            var path = reader.GetString(1);
                            var ext = reader.GetString(2);
                            var vector = Helper.ArrayToFloat((byte[])reader[3]);
                            var orientation = (RotateFlipType)(reader.GetByte(4));
                            var lastview = DateTime.FromBinary(reader.GetInt64(5));
                            var next = reader.GetString(6);
                            var horizon = reader.GetString(7);
                            var prev = reader.GetString(8);
                            var lastcheck = DateTime.FromBinary(reader.GetInt64(9));
                            var verified = reader.GetBoolean(10);
                            var counter = reader.GetInt32(11);
                            var taken = DateTime.FromBinary(reader.GetInt64(12));
                            var meta = reader.GetInt16(13);
                            var img = new Img(
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
            lock (_lock) {
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
            lock (_lock) {
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

        /*
        public static void Populate(IProgress<string> progress)
        {
        }
        */
        
        public static void AddImg(Img img)
        {
            lock (_lock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    var sb = new StringBuilder();
                    sb.Append($"INSERT INTO {AppConsts.TableImages} (");
                    sb.Append($"{AppConsts.AttributeHash},");
                    sb.Append($"{AppConsts.AttributePath},");
                    sb.Append($"{AppConsts.AttributeExt},");
                    sb.Append($"{AppConsts.AttributeVector},");
                    sb.Append($"{AppConsts.AttributeOrientation},");
                    sb.Append($"{AppConsts.AttributeLastView},");
                    sb.Append($"{AppConsts.AttributeNext},");
                    sb.Append($"{AppConsts.AttributePrev},");
                    sb.Append($"{AppConsts.AttributeHorizon},");
                    sb.Append($"{AppConsts.AttributeLastCheck},");
                    sb.Append($"{AppConsts.AttributeVerified},");
                    sb.Append($"{AppConsts.AttributeCounter},");
                    sb.Append($"{AppConsts.AttributeTaken},");
                    sb.Append($"{AppConsts.AttributeMeta}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash},");
                    sb.Append($"@{AppConsts.AttributePath},");
                    sb.Append($"@{AppConsts.AttributeExt},");
                    sb.Append($"@{AppConsts.AttributeVector},");
                    sb.Append($"@{AppConsts.AttributeOrientation},");
                    sb.Append($"@{AppConsts.AttributeLastView},");
                    sb.Append($"@{AppConsts.AttributeNext},");
                    sb.Append($"@{AppConsts.AttributePrev},");
                    sb.Append($"@{AppConsts.AttributeHorizon},");
                    sb.Append($"@{AppConsts.AttributeLastCheck},");
                    sb.Append($"@{AppConsts.AttributeVerified},");
                    sb.Append($"@{AppConsts.AttributeCounter},");
                    sb.Append($"@{AppConsts.AttributeTaken},");
                    sb.Append($"@{AppConsts.AttributeMeta}");
                    sb.Append(')');
                    sqlCommand.CommandText = sb.ToString();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", img.Hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributePath}", img.Path);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeExt}", img.Ext);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", Helper.ArrayFromFloat(img.GetVector()));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}", (byte)(img.Orientation));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", img.Next);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributePrev}", img.Prev);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHorizon}", img.Horizon);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", img.LastCheck.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", img.Verified);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeCounter}", img.Counter);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeTaken}", img.Taken.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMeta}", img.Meta);
                    sqlCommand.ExecuteNonQuery();
                }

                _imgList.Add(img.Hash, img);
            }
        }

        public static int ImgCount()
        {
            int count;
            lock (_lock) {
                count = _imgList.Count;
            }

            return count;
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
                if (!TryGetImg(next, out _)) {
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
                _scope = Math.Min(_imgList.Count, _imgList.Count(e => e.Value.Counter > 0) + 100);
                foreach (var imgX in _imgList.Values.Take(_scope)) {
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

        public static void GetNextView(out string bestHash, out string status)
        {
            bestHash = null;
            status = null;
            var total = ImgCount();
            var prefix = new[]{ "+", "0000", "000", "00", "0", "R" };
            var counters = new[] { 0, 0, 0, 0, 0, 0 };
            var candidates = new Img[]{ null, null, null, null, null, null };
            lock (_lock) {
                foreach (var img in _imgList.Values.Take(_scope)) {
                    if (!IsValid(img)) {
                        continue;
                    }

                    int category;
                    if (!img.Verified) {
                        category = 0;
                    }
                    else {
                        if (img.Next.StartsWith("0000")) {
                            category = 1;
                        }
                        else {
                            if (img.Next.StartsWith("000")) {
                                category = 2;
                            }
                            else {
                                if (img.Next.StartsWith("00")) {
                                    category = 3;
                                }
                                else {
                                    if (img.Next.StartsWith("0")) {
                                        category = 4;
                                    }
                                    else {
                                        category = 5;
                                    }
                                }
                            }
                        }

                    }

                    counters[category]++;
                    if (candidates[category] == null || img.LastView < candidates[category].LastView) {
                        candidates[category] = img;
                    }
                }

                if (candidates[0] == null && candidates[1] == null && candidates[2] == null &&
                    candidates[3] == null && candidates[4] == null && candidates[5] == null) {
                    status = "no candidates";
                    return;
                }

                while (bestHash == null) {
                    var rindex = 0;
                    while (candidates[rindex] == null) {
                        rindex++;
                    }

                    bestHash = candidates[rindex].Hash;
                    status = $"{prefix[rindex]}:{counters[rindex]}/{_scope}/{total}";

                    /*
                    var rindex = AppVars.RandomNext(6);
                    if (candidates[rindex] != null) {
                        bestHash = candidates[rindex].Hash;
                        status = $"{prefix[rindex]}:{counters[rindex]}/{total}";
                    }
                    */
                }
            }
        }

        public static string GetHashY(string hashX)
        {
            if (!TryGetImg(hashX, out var imgX)) {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(imgX.Prev) && TryGetImg(imgX.Prev.Substring(4), out var imgP)) {
                if (AppVars.RandomNext(10) == 0) {
                    return imgP.Hash;
                }

                if (!string.IsNullOrWhiteSpace(imgX.Next) && TryGetImg(imgX.Next.Substring(4), out var imgN)) {
                    return imgN.Hash;
                }

                return imgP.Hash;
            }
            else {
                if (!string.IsNullOrWhiteSpace(imgX.Next) && TryGetImg(imgX.Next.Substring(4), out var imgN)) {
                    return imgN.Hash;
                }

                return null;
            }
        }

        public static void Find(Img imgX, out string radiusNext, out string radiusPrev, out int counter)
        {
            radiusNext = null;
            radiusPrev = null;
            counter = 0;
            var lastviewPrev = DateTime.MaxValue;
            var vectorX = imgX.GetVector();
            Img[] copy;
            lock (_lock) {
                copy = _imgList.Values.Take(_scope).ToArray();
            }

            foreach (var img in copy) {
                if (imgX.Hash.Equals(img.Hash)) {
                    continue;
                }

                var vectorY = img.GetVector();
                var distance = VitHelper.GetDistance(vectorX, vectorY);
                var radius = Helper.GetRadius(img.Hash, distance);
                if (string.IsNullOrEmpty(imgX.Horizon) || (!string.IsNullOrEmpty(imgX.Horizon) &&
                                                           string.CompareOrdinal(radius, imgX.Horizon) > 0)) {
                    if (radiusNext == null || string.CompareOrdinal(radius, radiusNext) < 0) {
                        radiusNext = radius;
                    }
                }

                if (!string.IsNullOrEmpty(imgX.Horizon) && string.CompareOrdinal(radius, imgX.Horizon) <= 0) {
                    counter++;
                    if (radiusPrev == null || img.LastView < lastviewPrev) {
                        radiusPrev = radius;
                        lastviewPrev = img.LastView;
                    }
                }
            }
        }

        public static void SetVector(string hash, float[] vector)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.SetVector(vector);
                    ImgUpdateProperty(hash, AppConsts.AttributeVector, Helper.ArrayFromFloat(img.GetVector()));
                }
            }
        }

        public static void SetOrientation(string hash, RotateFlipType orientation)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.Orientation = orientation;
                    ImgUpdateProperty(hash, AppConsts.AttributeOrientation, (byte)img.Orientation);
                }
            }
        }

        public static void SetLastView(string hash)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.LastView = DateTime.Now;
                    ImgUpdateProperty(hash, AppConsts.AttributeLastView, img.LastView.Ticks);
                }
            }
        }

        public static void SetVerified(string hash)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.Verified = true;
                    ImgUpdateProperty(hash, AppConsts.AttributeVerified, img.Verified);
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
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.LastCheck = lastcheck;
                    ImgUpdateProperty(hash, AppConsts.AttributeLastCheck, img.LastCheck.Ticks);
                }
            }
        }

        public static void SetNext(string hash, string next)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.Next = next;
                    ImgUpdateProperty(hash, AppConsts.AttributeNext, img.Next);
                }
            }
        }

        public static void SetPrev(string hash, string prev)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.Prev = prev;
                    ImgUpdateProperty(hash, AppConsts.AttributePrev, img.Prev);
                }
            }
        }

        public static void SetHorizon(string hash)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.Horizon = img.Next;
                    ImgUpdateProperty(hash, AppConsts.AttributeHorizon, img.Horizon);
                }
            }
        }

        public static void SetCounter(string hash, int counter)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.Counter = counter;
                    ImgUpdateProperty(hash, AppConsts.AttributeCounter, img.Counter);
                }
            }
        }

        public static void SetPath(string hash, string path)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.Path = path;
                    ImgUpdateProperty(hash, AppConsts.AttributePath, img.Path);
                }
            }
        }

        public static void SetExt(string hash, string ext)
        {
            lock (_lock) {
                if (_imgList.TryGetValue(hash, out var img)) {
                    img.Ext = ext;
                    ImgUpdateProperty(hash, AppConsts.AttributeExt, img.Ext);
                }
            }
        }
    }
}