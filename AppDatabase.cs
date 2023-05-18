using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading;

namespace ImgSoh
{
    public static class AppDatabase
    {
        private static readonly SqlConnection _sqlConnection;
        private static readonly object _sqlLock = new object();

        static AppDatabase()
        {
            var connectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={AppConsts.FileDatabase};Connection Timeout=300";
            _sqlConnection = new SqlConnection(connectionString);
            _sqlConnection.Open();
        }

        public static void ImageUpdateProperty(string hash, string key, object val)
        {
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = $"UPDATE {AppConsts.TableImages} SET {key} = @{key} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                        sqlCommand.Parameters.AddWithValue($"@{key}", val);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                catch (SqlException) {
                    throw;
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void VarsUpdateProperty(string key, object val)
        {
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sqltext = $"UPDATE {AppConsts.TableVars} SET {key} = @{key}";
                    using (var sqlCommand = new SqlCommand(sqltext, _sqlConnection)) {
                        sqlCommand.Parameters.AddWithValue($"@{key}", val);
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                catch (SqlException) {
                    throw;
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void DeleteImage(string hash)
        {
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = $"DELETE FROM {AppConsts.TableImages} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                catch (SqlException) {
                    throw;
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void AddImage(Img img)
        {
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        var sb = new StringBuilder();
                        sb.Append($"INSERT INTO {AppConsts.TableImages} (");
                        sb.Append($"{AppConsts.AttributeHash}, ");
                        sb.Append($"{AppConsts.AttributeFolder}, ");
                        sb.Append($"{AppConsts.AttributeDateTaken}, ");
                        sb.Append($"{AppConsts.AttributeVector}, ");
                        sb.Append($"{AppConsts.AttributeLastView}, ");
                        sb.Append($"{AppConsts.AttributeOrientation}, ");
                        sb.Append($"{AppConsts.AttributeDistance}, ");
                        sb.Append($"{AppConsts.AttributeLastCheck}, ");
                        sb.Append($"{AppConsts.AttributeReview}, ");
                        sb.Append($"{AppConsts.AttributeNext}");
                        sb.Append(") VALUES (");
                        sb.Append($"@{AppConsts.AttributeHash}, ");
                        sb.Append($"@{AppConsts.AttributeFolder}, ");
                        sb.Append($"@{AppConsts.AttributeDateTaken}, ");
                        sb.Append($"@{AppConsts.AttributeVector}, ");
                        sb.Append($"@{AppConsts.AttributeLastView}, ");
                        sb.Append($"@{AppConsts.AttributeOrientation}, ");
                        sb.Append($"@{AppConsts.AttributeDistance}, ");
                        sb.Append($"@{AppConsts.AttributeLastCheck}, ");
                        sb.Append($"@{AppConsts.AttributeReview}, ");
                        sb.Append($"@{AppConsts.AttributeNext}");
                        sb.Append(')');
                        sqlCommand.CommandText = sb.ToString();
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", img.Hash);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFolder}", img.Folder);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeDateTaken}", img.DateTaken);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", img.GetVector());
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}", Helper.RotateFlipTypeToByte(img.Orientation));
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeDistance}", img.Distance);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", img.LastCheck);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeReview}", img.Review);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", img.Next);
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                catch (SqlException) {
                    throw;
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void LoadImages(IProgress<string> progress)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append($"{AppConsts.AttributeHash}, "); // 0
            sb.Append($"{AppConsts.AttributeFolder}, "); // 1
            sb.Append($"{AppConsts.AttributeDateTaken}, "); // 2
            sb.Append($"{AppConsts.AttributeVector}, "); // 3
            sb.Append($"{AppConsts.AttributeLastView}, "); // 4
            sb.Append($"{AppConsts.AttributeOrientation}, "); // 5
            sb.Append($"{AppConsts.AttributeDistance}, "); // 6
            sb.Append($"{AppConsts.AttributeLastCheck}, "); // 7
            sb.Append($"{AppConsts.AttributeReview}, "); // 8
            sb.Append($"{AppConsts.AttributeNext} "); // 9
            sb.Append($"FROM {AppConsts.TableImages}");
            var sqltext = sb.ToString();
            using (var sqlCommand = _sqlConnection.CreateCommand()) {
                sqlCommand.Connection = _sqlConnection;
                sqlCommand.CommandText = sqltext;
                using (var reader = sqlCommand.ExecuteReader()) {
                    var dtn = DateTime.Now;
                    while (reader.Read()) {
                        var hash = reader.GetString(0);
                        var folder = reader.GetString(1);
                        var datetaken = reader.GetDateTime(2);
                        var vector = (byte[])reader[3];
                        var lastview = reader.GetDateTime(4);
                        var orientation = Helper.ByteToRotateFlipType(reader.GetByte(5));
                        var distance = reader.GetFloat(6);
                        var lastcheck = reader.GetDateTime(7);
                        var review = reader.GetInt16(8);
                        var next = reader.GetString(9);
                        var img = new Img(
                            hash: hash,
                            folder: folder,
                            datetaken: datetaken,
                            vector: vector,
                            lastview: lastview,
                            orientation: orientation,
                            distance: distance,
                            lastcheck: lastcheck,
                            review: review,
                            next: next
                            );

                        AppImgs.Add(img);

                        if (DateTime.Now.Subtract(dtn).TotalMilliseconds > AppConsts.TimeLapse) {
                            dtn = DateTime.Now;
                            var count = AppImgs.Count();
                            progress?.Report($"Loading images ({count}){AppConsts.CharEllipsis}");
                        }
                    }
                }
            }

            progress?.Report("Loading vars...");

            sb.Length = 0;
            sb.Append("SELECT ");
            sb.Append($"{AppConsts.AttributeDateTakenLast} "); // 0
            sb.Append($"FROM {AppConsts.TableVars}");
            sqltext = sb.ToString();
            using (var sqlCommand = new SqlCommand(sqltext, _sqlConnection)) {
                using (var reader = sqlCommand.ExecuteReader()) {
                    while (reader.Read()) {
                        var dateTakenLast = reader.GetDateTime(0);
                        AppVars.DateTakenLast = dateTakenLast;
                        break;
                    }
                }
            }

            progress?.Report("Database loaded");
        }
    }
}
