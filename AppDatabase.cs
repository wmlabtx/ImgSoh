using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
                sb.Append($"{AppConsts.AttributeDistance}, "); // 6
                sb.Append($"{AppConsts.AttributeLastCheck}, "); // 7
                sb.Append($"{AppConsts.AttributeVerified}, "); // 8
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
                            var vector = (byte[])reader[2];
                            var orientation = Helper.ByteToRotateFlipType(reader.GetByte(3));
                            var lastview = reader.GetDateTime(4);
                            var next = reader.GetString(5);
                            var distance = reader.GetFloat(6);
                            var lastcheck = reader.GetDateTime(7);
                            var verified = reader.GetBoolean(8);
                            var history = reader.GetString(9);
                            var img = new Img(
                                hash: hash,
                                folder: folder,
                                vector: vector,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                distance: distance,
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

        public static void ImgUpdateProperty(string hash, string key, object val)
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
                    sb.Append($"{AppConsts.AttributeDistance}, ");
                    sb.Append($"{AppConsts.AttributeLastCheck}, ");
                    sb.Append($"{AppConsts.AttributeVerified}, ");
                    sb.Append($"{AppConsts.AttributeHistory}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash}, ");
                    sb.Append($"@{AppConsts.AttributeFolder}, ");
                    sb.Append($"@{AppConsts.AttributeVector}, ");
                    sb.Append($"@{AppConsts.AttributeOrientation}, ");
                    sb.Append($"@{AppConsts.AttributeLastView}, ");
                    sb.Append($"@{AppConsts.AttributeNext}, ");
                    sb.Append($"@{AppConsts.AttributeDistance}, ");
                    sb.Append($"@{AppConsts.AttributeLastCheck}, ");
                    sb.Append($"@{AppConsts.AttributeVerified}, ");
                    sb.Append($"@{AppConsts.AttributeHistory}");
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

        public static string GetNextCheck()
        {
            Img imgCheck = null;
            lock (_sqlLock) {
                foreach (var img in _imgList.Values) {
                    if (
                        img.GetVector() == null ||
                        img.GetVector().Length != 4096 ||
                        img.Next.Equals(img.Hash) ||
                        !_imgList.ContainsKey(img.Next) ||
                        img.IsInHistory(img.Next)) {
                        imgCheck = img;
                        break;
                    }

                    if (imgCheck == null || img.LastCheck < imgCheck.LastCheck) {
                        imgCheck = img;
                    }
                }
            }

            return imgCheck?.Hash;
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

        public static DateTime GetMinLastCheck()
        {
            DateTime lc;
            lock (_sqlLock) {
                lc = _imgList.Min(e => e.Value.LastCheck).AddSeconds(-1);
            }

            return lc;
        }

        public static string GetNextView()
        {
            string hashN = null;
            var mindistance = float.MaxValue;
            var hashes = new SortedList<int, Tuple<string, int, DateTime>>();
            
            lock (_sqlLock) {
                foreach (var imgX in _imgList.Values) {
                    if (imgX.GetVector() == null ||
                        imgX.GetVector().Length != 4096 ||
                        imgX.Next.Equals(imgX.Hash) ||
                        !_imgList.ContainsKey(imgX.Next) ||
                        imgX.IsInHistory(imgX.Next)) {
                        continue;
                    }

                    if (imgX.Verified) {
                        var hc = Math.Min(2, imgX.HistoryCount);
                        var imgY = _imgList[imgX.Next];
                        if (!hashes.TryGetValue(hc, out var e)) {
                            hashes.Add(hc, Tuple.Create(imgX.Hash, imgY.HistoryCount, imgX.LastView));
                        }
                        else {
                            if (imgY.HistoryCount < e.Item2) {
                                hashes[hc] = Tuple.Create(imgX.Hash, imgY.HistoryCount, imgX.LastView);
                            }
                            else {
                                if (imgY.HistoryCount > e.Item2) {
                                    continue;
                                }

                                if (imgX.LastView < e.Item3) {
                                    hashes[hc] = Tuple.Create(imgX.Hash, imgY.HistoryCount, imgX.LastView);
                                }
                            }
                        }
                    }
                    else {
                        if (imgX.Distance < mindistance) {
                            hashN = imgX.Hash;
                            mindistance = imgX.Distance;
                        }
                    }
                }

                if (hashN != null) {
                    var coin = AppVars.RandomNext(10);
                    if (coin == 0) {
                        return hashN;
                    }
                }

                if (hashes.Count > 0) {
                    var coin = AppVars.RandomNext(hashes.Count);
                    return hashes[coin].Item1;
                }
            }

            return null;
        }

        /*
        public static string[] GetFamily(int family)
        {
            string[] result;
            lock (_sqlLock) {
                result = _imgList.Where(e => e.Value.Family == family).Select(e => e.Key).ToArray();
            }

            return result;
        }

        public static int SuggestFamilyId()
        {
            var familyId = 1;
            lock (_sqlLock) {
                var families = _imgList
                    .Where(e => e.Value.Family > 0)
                    .Select(e => e.Value.Family)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToArray();

                while (familyId <= families.Length && families[familyId - 1] == familyId) {
                    familyId++;
                }
            }

            return familyId;
        }

        public static void RenameFamily(int of, int nf)
        {
            lock (_sqlLock) {
                foreach (var imgX in _imgList.Values) {
                    if (imgX.Family == of) {
                        imgX.SetFamily(nf);
                    }
                }
            }
        }
        */
    }
}