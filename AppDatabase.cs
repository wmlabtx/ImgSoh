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
                sb.Append($"{AppConsts.AttributeHash},"); // 0
                sb.Append($"{AppConsts.AttributeFolder},"); // 1
                sb.Append($"{AppConsts.AttributeVector},"); // 2
                sb.Append($"{AppConsts.AttributeOrientation},"); // 3
                sb.Append($"{AppConsts.AttributeLastView},"); // 4
                sb.Append($"{AppConsts.AttributeNext},"); // 5
                sb.Append($"{AppConsts.AttributeLastCheck},"); // 6
                sb.Append($"{AppConsts.AttributeVerified}, "); // 7
                sb.Append($"{AppConsts.AttributePrev},"); // 8
                sb.Append($"{AppConsts.AttributeHorizon},"); // 9
                sb.Append($"{AppConsts.AttributeCounter} "); // 10
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
                            var prev = reader.GetString(8);
                            var horizon = reader.GetString(9);
                            var counter = reader.GetInt32(10);
                            var img = new Img(
                                hash: hash,
                                folder: folder,
                                vector: vector,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                lastcheck: lastcheck,
                                verified: verified,
                                prev: prev,
                                horizon: horizon,
                                counter: counter
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
                    sb.Append($"{AppConsts.AttributeHash},");
                    sb.Append($"{AppConsts.AttributeFolder},");
                    sb.Append($"{AppConsts.AttributeVector},");
                    sb.Append($"{AppConsts.AttributeOrientation},");
                    sb.Append($"{AppConsts.AttributeLastView},");
                    sb.Append($"{AppConsts.AttributeNext},");
                    sb.Append($"{AppConsts.AttributeLastCheck},");
                    sb.Append($"{AppConsts.AttributeVerified},");
                    sb.Append($"{AppConsts.AttributePrev},");
                    sb.Append($"{AppConsts.AttributeHorizon},");
                    sb.Append($"{AppConsts.AttributeCounter}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash},");
                    sb.Append($"@{AppConsts.AttributeFolder},");
                    sb.Append($"@{AppConsts.AttributeVector},");
                    sb.Append($"@{AppConsts.AttributeOrientation},");
                    sb.Append($"@{AppConsts.AttributeLastView},");
                    sb.Append($"@{AppConsts.AttributeNext},");
                    sb.Append($"@{AppConsts.AttributeLastCheck},");
                    sb.Append($"@{AppConsts.AttributeVerified},");
                    sb.Append($"@{AppConsts.AttributePrev},");
                    sb.Append($"@{AppConsts.AttributeHorizon},");
                    sb.Append($"@{AppConsts.AttributeCounter}");
                    sb.Append(')');
                    sqlCommand.CommandText = sb.ToString();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", img.Hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFolder}", img.Folder);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", 
                        Helper.ArrayFromFloat(img.GetVector()));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}",
                        Helper.RotateFlipTypeToByte(img.Orientation));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", img.Next);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", img.LastCheck);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", img.Verified);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributePrev}", img.Prev);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHorizon}", img.Horizon);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeCounter}", img.Counter);
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
                    string.IsNullOrWhiteSpace(imgX.Next) ||
                    !_imgList.ContainsKey(imgX.Next.Substring(4)) ||
                    (!string.IsNullOrWhiteSpace(imgX.Prev) &&
                     !string.IsNullOrWhiteSpace(imgX.Horizon) &&
                     string.CompareOrdinal(imgX.Prev, imgX.Horizon) > 0) ||
                    (!string.IsNullOrWhiteSpace(imgX.Horizon) && 
                     !string.IsNullOrWhiteSpace(imgX.Next) && 
                     string.CompareOrdinal(imgX.Horizon, imgX.Next) >= 0)
                    ) {
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
            var cntNotVerified = 0;
            var cntPrev = 0;
            lock (_sqlLock) {
                total = _imgList.Count;
                foreach (var img in _imgList.Values) {
                    if (!IsValid(img)) {
                        continue; 
                    }

                    if (!img.Verified) {
                        cntNotVerified++;
                    }

                    if (img.Counter > 0) {
                        cntPrev++;
                    }

                    if (imgX == null) {
                        imgX = img;
                        continue;
                    }

                    if (img.Counter < imgX.Counter) {
                        imgX = img;
                        continue;
                    }

                    if (img.Counter > imgX.Counter) {
                        continue;
                    }

                    if (string.CompareOrdinal(img.Next, imgX.Next) < 0) {
                        imgX = img;
                        continue;
                    }
                }

                bestHash = imgX?.Hash;
            }

            var sb = new StringBuilder();
            sb.Append($"{cntNotVerified}/{cntPrev}/{total}");
            status = sb.ToString();
        }

        public static string GetHashY(string hashX)
        {
            if (!TryGetImg(hashX, out var imgX)) {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(imgX.Prev) && TryGetImg(imgX.Prev.Substring(4), out var imgP)) {
                if (AppVars.RandomNext(100) == 0) {
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
                        prev: o.Prev,
                        horizon: o.Horizon,
                        counter: o.Counter
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
                        prev: o.Prev,
                        horizon: o.Horizon,
                        counter: o.Counter
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
                        prev: o.Prev,
                        horizon: o.Horizon,
                        counter: o.Counter
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
                            prev: o.Prev,
                            horizon: o.Horizon,
                            counter: o.Counter
                        );

                        _imgList.Remove(hash);
                        _imgList.Add(hash, n);
                        ImgUpdateProperty(hash, AppConsts.AttributeVerified, n.Verified);
                    }
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
                        prev: o.Prev,
                        horizon: o.Horizon,
                        counter: o.Counter
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
                        prev: o.Prev,
                        horizon: o.Horizon,
                        counter: o.Counter
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeNext, n.Next);
                }
            }
        }

        public static void SetPrev(string hash, string prev)
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
                        prev: prev,
                        horizon: o.Horizon,
                        counter: o.Counter
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributePrev, n.Prev);
                }
            }
        }

        public static void SetHorizon(string hash)
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
                        prev: o.Prev,
                        horizon: o.Next,
                        counter: o.Counter
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributePrev, n.Prev);
                }
            }
        }

        public static void SetCounter(string hash, int counter)
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
                        prev: o.Prev,
                        horizon: o.Horizon,
                        counter: counter
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeCounter, n.Counter);
                }
            }
        }
    }
}