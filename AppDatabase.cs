﻿using System;
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
                sb.Append($"{AppConsts.AttributeFamily}, "); // 8
                sb.Append($"{AppConsts.AttributeHistory} "); // 9
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
                            var family = reader.GetInt32(8);
                            var history = reader.GetString(9);
                            var img = new Img(
                                hash: hash,
                                folder: folder,
                                vector: vector,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                lastcheck: lastcheck,
                                verified: verified,
                                family: family,
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
                    sb.Append($"{AppConsts.AttributeFamily}, ");
                    sb.Append($"{AppConsts.AttributeHistory}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash}, ");
                    sb.Append($"@{AppConsts.AttributeFolder}, ");
                    sb.Append($"@{AppConsts.AttributeVector}, ");
                    sb.Append($"@{AppConsts.AttributeOrientation}, ");
                    sb.Append($"@{AppConsts.AttributeLastView}, ");
                    sb.Append($"@{AppConsts.AttributeNext}, ");
                    sb.Append($"@{AppConsts.AttributeLastCheck}, ");
                    sb.Append($"@{AppConsts.AttributeVerified}, ");
                    sb.Append($"@{AppConsts.AttributeFamily}, ");
                    sb.Append($"@{AppConsts.AttributeHistory}");
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
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFamily}", img.Family);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHistory}", img.GetHistory());
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
                    imgX.Next.Equals(imgX.Hash) ||
                    imgX.IsInHistory(imgX.Next)) {
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
            string hash = null;
            var bestLastCheck = DateTime.MaxValue;
            lock (_sqlLock) {
                foreach (var e in _imgList.Values) {
                    if (
                        e.GetVector().Length != AppConsts.VectorLength || 
                        e.Hash.Equals(e.Next) ||
                        !_imgList.ContainsKey(e.Next)) {
                        return e.Hash;
                    }

                    if (e.Family > 0) {
                        var n = _imgList[e.Next];
                        if (e.Family == n.Family) {
                            return e.Hash;
                        }
                    }

                    if (e.LastCheck < bestLastCheck) {
                        bestLastCheck = e.LastCheck;
                        hash = e.Hash;
                    }
                }
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
            int total;
            lock (_sqlLock) {
                total = _imgList.Count;
                var familyList = new SortedList<int, DateTime>();
                foreach (var imgX in _imgList.Values) {
                    if (imgX.Family == 0 || !IsValid(imgX)) {
                        continue;
                    }

                    if (familyList.ContainsKey(imgX.Family)) {
                        if (imgX.LastView > familyList[imgX.Family]) {
                            familyList[imgX.Family] = imgX.LastView;
                        }
                    }
                    else {
                        familyList.Add(imgX.Family, imgX.LastView);
                    }
                }

                if (familyList.Count == 0) {
                    familyList.Add(0, DateTime.Now);
                }

                var families = familyList.OrderBy(e => e.Value).Select(e => e.Key).ToArray();
                var index = 0;
                while (bestHash == null && index < families.Length) {
                    var family = families[index];
                    var rindex = AppVars.RandomNext(100);
                    if (rindex == 0) {
                        family = 0;
                    }

                    var lastview = DateTime.MaxValue;
                    foreach (var e in _imgList.Where(e => e.Value.Family == family)) {
                        if (!IsValid(e.Value)) {
                            continue;
                        }

                        if (!e.Value.Verified) {
                            bestHash = e.Key;
                            break;
                        }

                        if (e.Value.LastView < lastview) {
                            bestHash = e.Key;
                            lastview = e.Value.LastView;
                        }
                    }

                    index++;
                }
            }

            var sb = new StringBuilder();
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
                        orientation: o.Orientation,
                        lastview: o.LastView,
                        next: o.Next,
                        lastcheck: o.LastCheck,
                        verified: o.Verified,
                        family: o.Family,
                        history: o.GetHistory()
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
                        family: o.Family, 
                        history: o.GetHistory()
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
                        family: o.Family,
                        history: o.GetHistory()
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
                            family: o.Family,
                            history: o.GetHistory()
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
                        family: o.Family,
                        history: o.GetHistory()
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
                        family: o.Family,
                        history: o.GetHistory()
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeNext, n.Next);
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
                        family: family,
                        history: string.Empty
                    );

                    _imgList.Remove(hash);
                    _imgList.Add(hash, n);
                    ImgUpdateProperty(hash, AppConsts.AttributeFamily, n.Family);
                    ImgUpdateProperty(hash, AppConsts.AttributeHistory, n.GetHistory());
                }
            }
        }

        public static void SetFamilyForced(string hash)
        {
            lock (_sqlLock) {
                if (_imgList.TryGetValue(hash, out var o)) {
                    if (o.Family == 0) {
                        var family = GetNewFamily();
                        var n = new Img(
                            hash: o.Hash,
                            folder: o.Folder,
                            vector: o.GetVector(),
                            orientation: o.Orientation,
                            lastview: o.LastView,
                            next: o.Next,
                            lastcheck: o.LastCheck,
                            verified: o.Verified,
                            family: family,
                            history: o.GetHistory()
                        );

                        _imgList.Remove(hash);
                        _imgList.Add(hash, n);
                        ImgUpdateProperty(hash, AppConsts.AttributeFamily, n.Family);
                    }
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

        public static string[] GetFamily(int family)
        {
            string[] array;
            lock (_sqlLock) {
                array = _imgList.Where(e => e.Value.Family == family).Select(e => e.Key).ToArray();
            }

            return array;
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
                            family: o.Family,
                            history: history
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
                            family: o.Family,
                            history: history
                        );

                        _imgList.Remove(hash);
                        _imgList.Add(hash, n);
                        ImgUpdateProperty(hash, AppConsts.AttributeHistory, n.GetHistory());
                    }
                }
            }
        }
    }
}