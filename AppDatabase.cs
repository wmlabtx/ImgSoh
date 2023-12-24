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
                sb.Append($"{AppConsts.AttributeColorVector}, "); // 10
                sb.Append($"{AppConsts.AttributeDateTaken} "); // 11
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
                            var colorvector = (byte[])reader[10];
                            var datetaken = reader.GetDateTime(11);
                            var img = new Img(
                                hash: hash,
                                folder: folder,
                                vector: vector,
                                colorvector: colorvector,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                distance: distance,
                                lastcheck: lastcheck,
                                verified: verified,
                                history: history,
                                datetaken: datetaken
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
                    sb.Append($"{AppConsts.AttributeColorVector}, ");
                    sb.Append($"{AppConsts.AttributeDateTaken}");
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
                    sb.Append($"@{AppConsts.AttributeColorVector}, ");
                    sb.Append($"@{AppConsts.AttributeDateTaken}");
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
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeColorVector}", img.GetColorVector());
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeDateTaken}", img.DateTaken);
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
                        imgX.GetColorVector() == null ||
                        imgX.GetColorVector().Length != 200 ||
                        imgX.DateTaken.Year == 1900 ||
                        imgX.Next.Equals(imgX.Hash) ||
                        imgX.IsInHistory(imgX.Next) ||
                        !_imgList.ContainsKey(imgX.Next)) {
                        imgCheck = imgX;
                        break;
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

        public static string GetNextView()
        {
            Img imgV = null;
            lock (_sqlLock) {
                var lvarray = _imgList.Values.OrderBy(e => e.LastView).ToArray();
                foreach (var imgX in lvarray) {
                    if (
                        imgX.GetVector() == null ||
                        imgX.GetVector().Length != 4096 ||
                        imgX.GetColorVector() == null ||
                        imgX.GetColorVector().Length != 200 ||
                        imgX.DateTaken.Year == 1900 ||
                        imgX.Next.Equals(imgX.Hash) ||
                        imgX.IsInHistory(imgX.Next) ||
                        !_imgList.ContainsKey(imgX.Next)) {
                        continue;
                    }

                    if (imgV == null) {
                        imgV = imgX;
                        continue;
                    }

                    if (imgX.Verified && !imgV.Verified) {
                        continue;
                    }

                    if (!imgX.Verified && imgV.Verified) {
                        imgV = imgX;
                        continue;
                    }

                    if (imgX.HistoryCount > imgV.HistoryCount) {
                        continue;
                    }

                    if (imgX.HistoryCount < imgV.HistoryCount) {
                        imgV = imgX;
                        continue;
                    }

                    if (imgX.Distance < imgV.Distance) {
                        imgV = imgX;
                    }
                }
            }

            return imgV?.Hash;
        }

        public static void GetCounters(out int good, out int bad, out float classDistance)
        {
            good = 0;
            bad = 0;
            Img imgV = null;
            var roundDistance = 100;
            lock (_sqlLock) {
                var lvarray = _imgList.Values.OrderBy(e => e.LastView).ToArray();
                foreach (var imgX in lvarray) {
                    if (
                        imgX.GetVector() == null ||
                        imgX.GetVector().Length != 4096 ||
                        imgX.GetColorVector() == null ||
                        imgX.GetColorVector().Length != 200 ||
                        imgX.DateTaken.Year == 1900 ||
                        imgX.Next.Equals(imgX.Hash) ||
                        imgX.IsInHistory(imgX.Next) ||
                        !_imgList.ContainsKey(imgX.Next)) {
                        bad++;
                        continue;
                    }

                    var distance = (int)Math.Round(imgX.Distance * 100f);

                    if (imgV == null) {
                        imgV = imgX;
                        good = 1;
                        roundDistance = distance;
                        continue;
                    }

                    if (imgX.Verified && !imgV.Verified) {
                        continue;
                    }

                    if (!imgX.Verified && imgV.Verified) {
                        imgV = imgX;
                        good = 1;
                        roundDistance = distance;
                        continue;
                    }

                    if (imgX.HistoryCount > imgV.HistoryCount) {
                        continue;
                    }

                    if (imgX.HistoryCount < imgV.HistoryCount) {
                        imgV = imgX;
                        good = 1;
                        roundDistance = distance;
                        continue;
                    }

                    if (distance > roundDistance) {
                        continue;
                    }

                    if (distance < roundDistance) {
                        imgV = imgX;
                        good = 1;
                        roundDistance = distance;
                        continue;
                    }

                    good++;
                }
            }

            classDistance = roundDistance / 100f;
        }
    }
}