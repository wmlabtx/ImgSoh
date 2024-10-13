using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImgSoh
{
    public static class AppImgs
    {
        private static readonly object _lock = new object();
        private static SQLiteConnection _sqlConnection;
        private static readonly SortedList<string, Img> _imgList = new SortedList<string, Img>(); // hash/img
        private static readonly SortedList<string, string> _nameList = new SortedList<string, string>(); // name/hash
        private static readonly List<long> _historyList = new List<long>();

        private static string GetSelect()
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append($"{AppConsts.AttributeHash},"); // 0
            sb.Append($"{AppConsts.AttributeName},"); // 1
            sb.Append($"{AppConsts.AttributeOrientation},"); // 2
            sb.Append($"{AppConsts.AttributeLastView},"); // 3
            sb.Append($"{AppConsts.AttributeNext},"); // 4
            sb.Append($"{AppConsts.AttributeVerified},"); // 5
            sb.Append($"{AppConsts.AttributeCounter},"); // 6
            sb.Append($"{AppConsts.AttributeTaken},"); // 7
            sb.Append($"{AppConsts.AttributeMeta},"); // 8
            sb.Append($"{AppConsts.AttributeVector},"); // 9
            sb.Append($"{AppConsts.AttributeMagnitude},"); // 10
            sb.Append($"{AppConsts.AttributeHorizon},"); // 11
            sb.Append($"{AppConsts.AttributeViewed}"); // 12
            return sb.ToString();
        }

        private static Img Get(IDataRecord reader)
        {
            var hash = reader.GetString(0);
            var name = reader.GetString(1);
            var orientation = (RotateFlipType)Enum.Parse(typeof(RotateFlipType), reader.GetInt64(2).ToString());
            var lastview = DateTime.FromBinary(reader.GetInt64(3));
            var next = reader.GetString(4);
            var verified = reader.GetBoolean(5);
            var counter = (int)reader.GetInt64(6);
            var taken = DateTime.FromBinary(reader.GetInt64(7));
            var meta = (int)reader.GetInt64(8);
            var vector = Helper.ArrayToFloat((byte[])reader[9]);
            var magnitude = reader.GetFloat(10);
            var horizon = reader.GetString(11);
            var viewed = (int)reader.GetInt64(12);
            var img = new Img(
                hash: hash,
                name: name,
                orientation: orientation,
                lastview: lastview,
                next: next,
                verified: verified,
                counter: counter,
                taken: taken,
                meta: meta,
                vector: vector,
                magnitude: magnitude,
                horizon: horizon,
                viewed: viewed
            );

            return img;
        }

        public static void LoadNamesAndVectors(string filedatabase, IProgress<string> progress)
        {
            lock (_lock) {
                _imgList.Clear();
                _nameList.Clear();
                var connectionString = $"Data Source={filedatabase};Version=3;";
                _sqlConnection = new SQLiteConnection(connectionString);
                _sqlConnection.Open();

                var sb = new StringBuilder(GetSelect());
                sb.Append($" FROM {AppConsts.TableImages};");
                using (var sqlCommand = new SQLiteCommand(sb.ToString(), _sqlConnection))
                using (var reader = sqlCommand.ExecuteReader()) {
                    if (!reader.HasRows) {
                        return;
                    }

                    var dtn = DateTime.Now;
                    while (reader.Read()) {
                        var img = Get(reader);
                        Add(img);
                        if (!(DateTime.Now.Subtract(dtn).TotalMilliseconds > AppConsts.TimeLapse)) {
                            continue;
                        }

                        dtn = DateTime.Now;
                        var count = AppImgs.Count();
                        progress?.Report($"Loading names and vectors ({count}){AppConsts.CharEllipsis}");
                    }
                }
            }
        }

        public static Img Get(string hash)
        {
            lock (_lock) {
                var sb = new StringBuilder(GetSelect());
                sb.Append($" FROM {AppConsts.TableImages} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}");
                using (var sqlCommand = new SQLiteCommand(sb.ToString(), _sqlConnection)) {
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    using (var reader = sqlCommand.ExecuteReader()) {
                        if (reader.Read()) {
                            var img = Get(reader);
                            return img;
                        }
                    }
                }
            }

            return null;
        }

        public static void Save(Img img)
        {
            lock (_lock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    var sb = new StringBuilder();
                    sb.Append($"INSERT INTO {AppConsts.TableImages} (");
                    sb.Append($"{AppConsts.AttributeHash},");
                    sb.Append($"{AppConsts.AttributeName},");
                    sb.Append($"{AppConsts.AttributeOrientation},");
                    sb.Append($"{AppConsts.AttributeLastView},");
                    sb.Append($"{AppConsts.AttributeNext},");
                    sb.Append($"{AppConsts.AttributeVerified},");
                    sb.Append($"{AppConsts.AttributeCounter},");
                    sb.Append($"{AppConsts.AttributeTaken},");
                    sb.Append($"{AppConsts.AttributeMeta},");
                    sb.Append($"{AppConsts.AttributeVector},");
                    sb.Append($"{AppConsts.AttributeMagnitude},");
                    sb.Append($"{AppConsts.AttributeHorizon},");
                    sb.Append($"{AppConsts.AttributeViewed}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash},");
                    sb.Append($"@{AppConsts.AttributeName},");
                    sb.Append($"@{AppConsts.AttributeOrientation},");
                    sb.Append($"@{AppConsts.AttributeLastView},");
                    sb.Append($"@{AppConsts.AttributeNext},");
                    sb.Append($"@{AppConsts.AttributeVerified},");
                    sb.Append($"@{AppConsts.AttributeCounter},");
                    sb.Append($"@{AppConsts.AttributeTaken},");
                    sb.Append($"@{AppConsts.AttributeMeta},");
                    sb.Append($"@{AppConsts.AttributeVector},");
                    sb.Append($"@{AppConsts.AttributeMagnitude},");
                    sb.Append($"@{AppConsts.AttributeHorizon},");
                    sb.Append($"@{AppConsts.AttributeViewed}");
                    sb.Append(')');
                    sqlCommand.CommandText = sb.ToString();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", img.Hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeName}", img.Name);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}", (int)img.Orientation);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", img.Next);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", img.Verified);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeCounter}", img.Counter);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeTaken}", img.Taken.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMeta}", img.Meta);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", Helper.ArrayFromFloat(img.Vector));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMagnitude}", img.Magnitude);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHorizon}", img.Horizon);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeViewed}", img.Viewed);
                    sqlCommand.ExecuteNonQuery();
                }

                Add(img);
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

        private static bool ContainsKey(string key)
        {
            bool result;
            lock (_lock) {
                result = key.Length == 32 ? 
                    _imgList.ContainsKey(key) : 
                    _nameList.ContainsKey(key);
            }

            return result;
        }

        public static string GetName(string hash)
        {
            string name;
            var length = 5;
            do {
                length++;
                name = hash.Substring(0, length);
            } while (ContainsKey(name));

            return name;
        }

        public static bool TryGet(string hash, out Img img)
        {
            lock (_lock) {
                return _imgList.TryGetValue(hash, out img);
            }
        }

        public static bool TryGetByName(string name, out Img img)
        {
            img = null;
            lock (_lock) {
                return _nameList.TryGetValue(name, out var hash) && TryGet(hash, out img);
            }
        }

        private static void Add(Img img)
        {
            lock (_lock) {
                _imgList.Add(img.Hash, img);
                _nameList.Add(img.Name, img.Hash);
            }
        }

        public static void Delete(string hash)
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
            }
        }

        public static void Remove(string key)
        {
            lock (_lock) {
                if (key.Length == 32) {
                    if (TryGet(key, out var img)) {
                        _imgList.Remove(key);
                        _nameList.Remove(img.Name);
                    }
                }
                else {
                    if (TryGetByName(key, out var img)) {
                        _imgList.Remove(img.Hash);
                        _nameList.Remove(key);
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

                Replace(Get(hash));
            }
        }

        private static void Replace(Img imgnew)
        {
            lock (_lock) {
                if (ContainsKey(imgnew.Hash)) {
                    Remove(imgnew.Hash);
                }

                Add(imgnew);
            }
        }

        public static void SetOrientation(string hash, RotateFlipType orientation)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeOrientation, (int)orientation);
        }

        private static void SetLastView(string hash, DateTime lastview)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeLastView, lastview.Ticks);
        }

        public static void SetNext(string hash, string next)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeNext, next);
        }

        public static void SetVerified(string hash)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeVerified, true);
        }

        public static void SetCounter(string hash, int counter)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeCounter, counter);
        }

        public static void SetVector(string hash, float[] vector)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeVector, Helper.ArrayFromFloat(vector));
        }

        public static void SetMagnitude(string hash, float magnitude)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeMagnitude, magnitude);
        }

        public static void SetHorizon(string hash, string horizon)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeHorizon, horizon);
        }

        private static void SetViewed(string hash, int viewed)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeViewed, viewed);
        }

        public static void UpdateViewed(string hash, int viewed)
        {
            SetViewed(hash, viewed + 1);
            SetLastView(hash, DateTime.Now);
        }

        public static bool IsValid(Img img)
        {
            lock (_lock) {
                if (img.Magnitude <=0f ||
                    string.IsNullOrWhiteSpace(img.Next) ||
                    (!string.IsNullOrWhiteSpace(img.Horizon) &&
                     !string.IsNullOrWhiteSpace(img.Next) &&
                     string.CompareOrdinal(img.Horizon, img.Next) >= 0)
                   ) {
                    return false;
                }

                var next = img.Next.Substring(4);
                if (!ContainsKey(next)) {
                    return false;
                }
            }

            return true;
        }

        public static Img GetForView()
        {
            Img imgX = null;
            lock (_lock) {
                long diffMax = 0;
                foreach (var img in _imgList.Values) {
                    if (!IsValid(img)) {
                        continue;
                    }

                    if (img.Next.Length > 4 && img.Next.StartsWith("00")) {
                        return img;
                    }

                    var diffMin = Math.Abs(DateTime.Now.Ticks - img.LastView.Ticks);
                    foreach (var ts in _historyList) {
                        var diff = Math.Abs(ts - img.LastView.Ticks);
                        if (diff < diffMin) {
                            diffMin = diff;
                        }
                    }

                    if (diffMin > diffMax && (imgX == null || img.Counter <= imgX.Counter)) {
                        imgX = img;
                        diffMax = diffMin;
                    }
                }

                if (imgX != null) {
                    _historyList.Add(imgX.LastView.Ticks);
                    while (_historyList.Count > 100) {
                        _historyList.RemoveAt(0);
                    }
                }
            }

            return imgX;
        }

        public static Img GetForCheck()
        {
            Img[] shadow;
            lock (_lock) {
                shadow = _imgList
                    .Select(e => e.Value)
                    .ToArray();
            }

            foreach (var img in shadow) {
                if (!IsValid(img)) {
                    return img;
                }
            }

            var irandom = AppVars.RandomNext(shadow.Length);
            return shadow[irandom];
        }

        public static void Find(Img img, out string radiusNext, out int counter)
        {
            lock (_lock) {
                var distances = new float[Count()];
                var hashList = _imgList.Select(e => e.Value.Hash).ToArray();
                var vectorList = _imgList.Select(e => e.Value.Vector).ToArray();
                var magnitudeList = _imgList.Select(e => e.Value.Magnitude).ToArray();
                var vx = img.Vector;
                var mx = img.Magnitude;
                Parallel.For(0, distances.Length, i => {
                        distances[i] = AppVit.GetDistance(vx, mx, vectorList[i], magnitudeList[i]);
                    });

                radiusNext = null;
                counter = 0;
                var hx = img.Hash;
                for (var i = 0; i < _imgList.Keys.Count; i++) {
                    if (hx.Equals(hashList[i])) {
                        continue;
                    }

                    var radius = Helper.GetRadius(hashList[i], distances[i]);
                    if (string.IsNullOrEmpty(img.Horizon) || (!string.IsNullOrEmpty(img.Horizon) && string.CompareOrdinal(radius, img.Horizon) > 0)) {
                        if (radiusNext == null || string.CompareOrdinal(radius, radiusNext) < 0) {
                            radiusNext = radius;
                        }
                    }

                    if (!string.IsNullOrEmpty(img.Horizon) && string.CompareOrdinal(radius, img.Horizon) <= 0) {
                        counter++;
                    }
                }
            }
        }

        public static DateTime GetMinimalLastView()
        {
            lock (_lock) {
                return _imgList.Min(e => e.Value.LastView).AddSeconds(-1);
            }
        }
    }
}
