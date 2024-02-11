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
                sb.Append($"{AppConsts.AttributeHistory}, "); // 9
                sb.Append($"{AppConsts.AttributeFingerPrint}, "); // 10
                sb.Append($"{AppConsts.AttributeMatch}, "); // 11
                sb.Append($"{AppConsts.AttributeFamily} "); // 12
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
                            var fingerprint = reader.GetString(10);
                            var match = reader.GetInt16(11);
                            var family = reader.GetInt16(12);
                            var img = new Img(
                                hash: hash,
                                folder: folder,
                                vector: vector,
                                fingerprint: fingerprint,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                distance: distance,
                                match: match,
                                lastcheck: lastcheck,
                                verified: verified,
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
                    sb.Append($"{AppConsts.AttributeHistory}, ");
                    sb.Append($"{AppConsts.AttributeFingerPrint}, ");
                    sb.Append($"{AppConsts.AttributeMatch}, ");
                    sb.Append($"{AppConsts.AttributeFamily} ");
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
                    sb.Append($"@{AppConsts.AttributeHistory}, ");
                    sb.Append($"@{AppConsts.AttributeFingerPrint}, ");
                    sb.Append($"@{AppConsts.AttributeMatch}, ");
                    sb.Append($"@{AppConsts.AttributeFamily} ");
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
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMatch}", img.Match);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", img.LastCheck);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", img.Verified);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFingerPrint}", img.FingerPrintString);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHistory}", img.History);
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

        public static string GetNextCheck()
        {
            Img imgCheck = null;
            lock (_sqlLock) {
                foreach (var imgX in _imgList.Values) {
                    if (
                        imgX.GetVector() == null ||
                        imgX.GetVector().Length != 4096 ||
                        imgX.FingerPrint.Length == 0 ||
                        imgX.Next.Equals(imgX.Hash) ||
                        imgX.IsInHistory(imgX.Next) ||
                        !_imgList.ContainsKey(imgX.Next) ||
                        imgX.Distance > 1f) {
                        imgCheck = imgX;
                        break;
                    }

                    if (imgX.Family > 0) {
                        foreach (var hashY in imgX.HistoryArray) {
                            if (_imgList.TryGetValue(hashY, out var imgY)) {
                                if (imgX.Family == imgY.Family) {
                                    imgCheck = imgX;
                                    break;
                                }
                            }
                            else {
                                imgCheck = imgX;
                                break;
                            }
                        }
                    }

                    if (imgCheck == null || imgX.LastCheck < imgCheck.LastCheck) {
                        imgCheck = imgX;
                    }
                }
            }

            return imgCheck?.Hash;
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
            var classes = new[] { new List<string>(), new List<string>(), new List<string>() };
            // 0 - bad pair
            // 1 - new image (!Verified)
            // 2 - family (Family > 0)

            int total;
            lock (_sqlLock) {
                total = _imgList.Count;
                foreach (var imgX in _imgList.Values) {
                    if (
                        imgX.GetVector() == null ||
                        imgX.GetVector().Length != 4096 ||
                        imgX.FingerPrint.Length == 0 ||
                        imgX.Next.Equals(imgX.Hash) ||
                        imgX.IsInHistory(imgX.Next) ||
                        imgX.Distance > 1f
                    ) {
                        classes[0].Add(imgX.Hash);
                        continue;
                    }

                    if (!_imgList.TryGetValue(imgX.Next, out var imgY)) {
                        classes[0].Add(imgX.Hash);
                        continue;
                    }

                    if (imgX.Family > 0) {
                        var valid = true;
                        foreach (var hashY in imgX.HistoryArray) {
                            if (_imgList.TryGetValue(hashY, out imgY)) {
                                if (imgX.Family == imgY.Family) {
                                    valid = false;
                                    break;
                                }
                            }
                            else {
                                valid = false;
                                break;
                            }
                        }

                        if (!valid) {
                            classes[0].Add(imgX.Hash);
                            continue;
                        }
                    }

                    if (!imgX.Verified) {
                        classes[1].Add(imgX.Hash);
                        continue;
                    }

                    if (imgX.Family > 0 || imgY.Family > 0) {
                        classes[2].Add(imgX.Hash);
                        continue;
                    } 
                }
            }

            if (classes[1].Count == 0 && classes[2].Count == 0) {
                return;
            }

            var sb = new StringBuilder();
            if (classes[0].Count > 0) {
                sb.Append($"b{classes[0].Count}");
            }

            if (classes[1].Count > 0) {
                if (sb.Length > 0) {
                    sb.Append('/');
                }

                sb.Append($"n{classes[1].Count}");
            }

            if (classes[2].Count > 0) {
                if (sb.Length > 0) {
                    sb.Append('/');
                }

                sb.Append($"f{classes[2].Count}");
            }

            if (sb.Length > 0) {
                sb.Append('/');
            }

            sb.Append($"{total}");
            status = sb.ToString();

            var classid = 0;
            while (classid <= 0) {
                classid = AppVars.RandomNext(2) + 1;
                if (classes[classid].Count == 0) {
                    classid = 0;
                }
            }

            var candidates = classes[classid];
            if (classid == 1) {
                // new images, random pick
                
                var rindex = AppVars.RandomNext(candidates.Count);
                bestHash = candidates[rindex];
                return;
            }

            if (classid == 2) {
                // family

                var bestX = 0;
                var bestY = 0;
                foreach (var hash in candidates) {
                    if (TryGetImg(hash, out var imgX)) {
                        var dayX = (int)Math.Round(DateTime.Now.Subtract(imgX.LastView).TotalHours);
                        if (TryGetImg(imgX.Next, out var imgY)) {
                            var dayY = (int)Math.Round(DateTime.Now.Subtract(imgY.LastView).TotalHours);
                            if (dayX > bestX || (dayX == bestX && dayY > bestY)) {
                                bestX = dayX;
                                bestY = dayY;
                                bestHash = hash;
                            }
                        }
                    }
                }
            }
        }

        public static string[] GetFamily(short family)
        {
            string[] array;
            lock (_sqlLock) {
                array = _imgList.Where(e => e.Value.Family == family).Select(e => e.Key).ToArray();
            }

            return array;
        }

        public static short GetNewFamily()
        {
            short[] families;
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

            return (short)(pos + 1);
        }
    }
}