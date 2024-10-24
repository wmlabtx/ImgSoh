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

        private static string GetSelect()
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append($"{AppConsts.AttributeHash},"); // 0
            sb.Append($"{AppConsts.AttributeName},"); // 1
            sb.Append($"{AppConsts.AttributeTaken},"); // 2
            sb.Append($"{AppConsts.AttributeMeta},"); // 3
            sb.Append($"{AppConsts.AttributeVector},"); // 4
            sb.Append($"{AppConsts.AttributeMagnitude},"); // 5
            sb.Append($"{AppConsts.AttributeOrientation},"); // 6
            sb.Append($"{AppConsts.AttributeLastView},"); // 7
            sb.Append($"{AppConsts.AttributeFamily},"); // 8
            sb.Append($"{AppConsts.AttributeHistory}"); // 9
            return sb.ToString();
        }

        private static Img Get(IDataRecord reader)
        {
            var hash = reader.GetString(0);
            var name = reader.GetString(1);
            var taken = DateTime.FromBinary(reader.GetInt64(2));
            var meta = (int)reader.GetInt64(3);
            var vector = Helper.ArrayToFloat((byte[])reader[4]);
            var magnitude = reader.GetFloat(5);
            var orientation = (RotateFlipType)Enum.Parse(typeof(RotateFlipType), reader.GetInt64(6).ToString());
            var lastview = DateTime.FromBinary(reader.GetInt64(7));
            var family = reader.GetString(8);
            var history = reader.GetString(9);
            var img = new Img(
                hash: hash,
                name: name,
                taken: taken,
                meta: meta,
                vector: vector,
                magnitude: magnitude,
                orientation: orientation,
                lastview: lastview,
                family: family,
                history: history
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

        private static Img Get(string hash)
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
                    sb.Append($"{AppConsts.AttributeTaken},");
                    sb.Append($"{AppConsts.AttributeMeta},");
                    sb.Append($"{AppConsts.AttributeVector},");
                    sb.Append($"{AppConsts.AttributeMagnitude},");
                    sb.Append($"{AppConsts.AttributeOrientation},");
                    sb.Append($"{AppConsts.AttributeLastView},");
                    sb.Append($"{AppConsts.AttributeFamily},");
                    sb.Append($"{AppConsts.AttributeHistory}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash},");
                    sb.Append($"@{AppConsts.AttributeName},");
                    sb.Append($"@{AppConsts.AttributeTaken},");
                    sb.Append($"@{AppConsts.AttributeMeta},");
                    sb.Append($"@{AppConsts.AttributeVector},");
                    sb.Append($"@{AppConsts.AttributeMagnitude},");
                    sb.Append($"@{AppConsts.AttributeOrientation},");
                    sb.Append($"@{AppConsts.AttributeLastView},");
                    sb.Append($"@{AppConsts.AttributeFamily},");
                    sb.Append($"@{AppConsts.AttributeHistory}");
                    sb.Append(')');
                    sqlCommand.CommandText = sb.ToString();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", img.Hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeName}", img.Name);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeTaken}", img.Taken.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMeta}", img.Meta);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", Helper.ArrayFromFloat(img.Vector));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMagnitude}", img.Magnitude);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}", (int)img.Orientation);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFamily}", img.Family);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHistory}", img.History);
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

        public static void SetVector(string hash, float[] vector)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeVector, Helper.ArrayFromFloat(vector));
        }

        public static void SetMagnitude(string hash, float magnitude)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeMagnitude, magnitude);
        }

        public static void SetOrientation(string hash, RotateFlipType orientation)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeOrientation, (int)orientation);
        }

        public static void SetLastView(string hash, DateTime lastview)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeLastView, lastview.Ticks);
        }

        public static void SetFamily(string hash, string family)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeFamily, family);
        }

        public static void SetHistory(string hash, string history)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeHistory, history);
        }

        public static void AddToHistory(string hash, string added)
        {
            if (TryGet(hash, out var img)) {
                if (!img.IsInHistory(added)) {
                    var history = img.History + added;
                    SetHistory(hash, history);
                }
            }
        }

        public static Img GetForView()
        {
            lock (_lock) {
                Img imgX;
                if (AppVars.RandomNext(10) > 0) {
                    var families = _imgList
                        .Select(e => e.Value.Family)
                        .Where(e => e.Length > 0)
                        .Distinct()
                        .ToArray();

                    if (families.Length > 0) {
                        var rindex = AppVars.RandomNext(families.Length);
                        var family = families[rindex];
                        imgX = _imgList
                            .Values
                            .Where(e => e.Family.Equals(family))
                            .OrderBy(e => e.LastView)
                            .First();
                        return imgX;
                    }
                }

                if (AppVars.RandomNext(5) > 0) {
                    var used = _imgList
                        .Values
                        .Where(e => e.Family.Length == 0 && e.History.Length > 0)
                        .ToArray();
                    if (used.Length > 0) {
                        imgX = used
                            .OrderBy(e => e.LastView)
                            .First();
                        return imgX;
                    }
                }

                var virgins = _imgList
                    .Values
                    .Where(e => e.Family.Length == 0 && e.History.Length == 0)
                    .ToArray();
                if (virgins.Length > 0) {
                    var rindex = AppVars.RandomNext(virgins.Length);
                    imgX = virgins[rindex];
                    return imgX;
                }

                imgX = _imgList
                    .Values
                    .OrderBy(e => e.LastView)
                    .First();
                return imgX;
            }
        }

        private static List<Tuple<string, float>> CalculateDistances(Img img, List<string> hashList, List<float[]> vectorList, List<float> magnitudeList)
        {
            var distances = new float[hashList.Count];
            var vx = img.Vector;
            var mx = img.Magnitude;
            Parallel.For(0, distances.Length, i => {
                distances[i] = AppVit.GetDistance(vx, mx, vectorList[i], magnitudeList[i]);
            });

            var vector = hashList.Select((t, i) => Tuple.Create(t, distances[i])).ToList();
            vector.Sort((x, y) => x.Item2.CompareTo(y.Item2));
            return vector;
        }

        public static List<Tuple<string, float>> GetVector(Img img)
        {
            lock (_lock) {
                if (img.History.Length > 0) {
                    var history = new List<string>();
                    foreach (var e in img.HistoryArray) {
                        if (_imgList.TryGetValue(e, out var imgY)) {
                            if (img.Family.Length == 0) {
                                history.Add(e);
                            }
                            else {
                                if (img.Family.Length > 0 && imgY.Family.Length == 0) {
                                    history.Add(e);
                                }
                                else {
                                    if (img.Family.Length > 0 && imgY.Family.Length > 0 && img.Family.Equals(imgY.Family)) {
                                        history.Add(e);
                                    }
                                }
                            }
                        }
                    }

                    var history_new = string.Join(string.Empty, history);
                    if (!history_new.Equals(img.History)) {
                        SetHistory(img.Hash, history_new);
                        img = _imgList[img.Hash];
                    }
                }

                var hashList = new List<string>();
                var vectorList = new List<float[]>();
                var magnitudeList = new List<float>();

                if (img.Family.Length == 0) {
                    var families = GetFamilies();
                    foreach (var e in img.HistoryArray) {
                        if (_imgList.TryGetValue(e, out var imgY)) {
                            if (imgY.Family.Length > 0) {
                                families.Remove(imgY.Family);
                            }
                        }
                    }

                    if (families.Count > 0) {
                        foreach (var family in families) {
                            var f = _imgList
                                .Values
                                .Where(e => e.Family.Equals(family))
                                .OrderBy(e => e.LastView)
                                .First();

                            hashList.Add(f.Hash);
                            vectorList.Add(f.Vector);
                            magnitudeList.Add(f.Magnitude);
                        }

                        return CalculateDistances(img, hashList, vectorList, magnitudeList);
                    }

                    families = GetFamilies();
                    string family_new;
                    do {
                        family_new = AppHash.GetFamily();
                    } while (families.Contains(family_new));
                    SetFamily(img.Hash, family_new);
                    img = _imgList[img.Hash];
                }

                foreach (var e in _imgList.Values) {
                    if (e.Hash.Equals(img.Hash) || img.IsInHistory(e.Hash)) {
                        continue;
                    }

                    if (img.Family.Length > 0 && e.Family.Length > 0 && e.Family != img.Family) {
                        continue;
                    }

                    hashList.Add(e.Hash);
                    vectorList.Add(e.Vector);
                    magnitudeList.Add(e.Magnitude);
                }

                return CalculateDistances(img, hashList, vectorList, magnitudeList);
            }
        }

        public static DateTime GetMinimalLastView()
        {
            lock (_lock) {
                return _imgList.Min(e => e.Value.LastView).AddSeconds(-1);
            }
        }

        private static HashSet<string> GetFamilies()
        {
            lock (_lock) {
                return new HashSet<string>(
                    _imgList
                        .Select(e => e.Value.Family)
                        .Where(e => e.Length > 0)
                        .Distinct()
                        .ToArray());
            }
        }

        public static int GetFamilySize(string family)
        {
            if (family.Length == 0) {
                return 0;
            }

            lock (_lock) {
                return _imgList.Count(e => e.Value.Family.Equals(family));
            }
        }
    }
}
