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
            var candidates = new List<Img>();
            lock (_sqlLock) {
                foreach (var img in _imgList.Values) {
                    if (img.GetVector() == null ||
                        img.GetVector().Length != 4096 ||
                        img.Next.Equals(img.Hash) ||
                        !_imgList.ContainsKey(img.Next) ||
                        img.IsInHistory(img.Next)) {
                        continue;
                    }

                    if (candidates.Count == 0) {
                        candidates.Add(img);
                    }
                    else {
                        if (img.HistoryCount < candidates[0].HistoryCount) {
                            candidates.Clear();
                            candidates.Add(img);
                        }
                        else {
                            if (img.HistoryCount > candidates[0].HistoryCount) {
                                continue;
                            }

                            candidates.Add(img);
                        }
                    }
                }
            }

            var mindistance = candidates.Min(e => e.Distance);
            var hash = candidates.Find(e => Math.Abs(e.Distance - mindistance) < 0.0001f).Hash;

            /*
            string hash = null;
            var scope = candidates.OrderBy(e => e.LastView).Take(100).ToArray();
            var minlv = DateTime.MaxValue;
            var minhc = int.MaxValue;
            foreach (var imgX in scope) {
                if (TryGetImg(imgX.Next, out var imgY)) {
                    if (imgY.HistoryCount < minhc || (imgY.HistoryCount == minhc && imgY.LastView < minlv)) {
                        minlv = imgY.LastView;
                        minhc = imgY.HistoryCount;
                        hash = imgX.Hash;
                    }
                }
            }
            */

            return hash;
        }

        /*
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
        */
    }
}