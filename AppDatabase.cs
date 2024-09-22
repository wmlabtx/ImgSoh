using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data.SQLite;
using System.Text;
using System.Diagnostics.Metrics;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Forms;

namespace ImgSoh
{
    public static class AppDatabase
    {
        private static readonly object _lock = new object();
        private static SQLiteConnection _sqlConnection;

        public static void LoadNamesAndVectors(string filedatabase, IProgress<string> progress)
        {
            lock (_lock) {
                AppImgs.Clear();
                var connectionString = $"Data Source={filedatabase};Version=3;";
                _sqlConnection = new SQLiteConnection(connectionString);
                _sqlConnection.Open();

                var sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append($"{AppConsts.AttributeHash},"); // 0
                sb.Append($"{AppConsts.AttributeName},"); // 1
                sb.Append($"{AppConsts.AttributeVector},"); // 2
                sb.Append($"{AppConsts.AttributeMagnitude}"); // 3
                sb.Append($" FROM {AppConsts.TableImages};");
                using (var sqlCommand = new SQLiteCommand(sb.ToString(), _sqlConnection))
                using (var reader = sqlCommand.ExecuteReader()) {
                    if (!reader.HasRows) {
                        return;
                    }

                    var dtn = DateTime.Now;
                    while (reader.Read()) {
                        var hash = reader.GetString(0);
                        var name = reader.GetString(1);
                        var vector = Helper.ArrayToFloat((byte[])reader[2]);
                        var magnitude = reader.GetFloat(3);
                        AppImgs.Add(hash:hash, name:name, vector:vector, magnitude:magnitude);
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

        public static Img GetImg(string hash)
        {
            Img img = null;
            lock (_lock) {
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append($"{AppConsts.AttributeName},"); // 0
                sb.Append($"{AppConsts.AttributeOrientation},"); // 1
                sb.Append($"{AppConsts.AttributeLastView},"); // 2
                sb.Append($"{AppConsts.AttributeNext},"); // 3
                sb.Append($"{AppConsts.AttributeLastCheck},"); // 4
                sb.Append($"{AppConsts.AttributeVerified},"); // 5
                sb.Append($"{AppConsts.AttributeCounter},"); // 6
                sb.Append($"{AppConsts.AttributeTaken},"); // 7
                sb.Append($"{AppConsts.AttributeMeta},"); // 8
                sb.Append($"{AppConsts.AttributeMagnitude},"); // 9
                sb.Append($"{AppConsts.AttributeHorizon},"); // 10
                sb.Append($"{AppConsts.AttributeViewed}"); // 11
                sb.Append($" FROM {AppConsts.TableImages} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}");
                using (var sqlCommand = new SQLiteCommand(sb.ToString(), _sqlConnection)) {
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    using (var reader = sqlCommand.ExecuteReader()) {
                        if (reader.Read()) {
                            var name = reader.GetString(0);
                            var orientation = (RotateFlipType)Enum.Parse(typeof(RotateFlipType), reader.GetInt64(1).ToString());
                            var lastview = DateTime.FromBinary(reader.GetInt64(2));
                            var next = reader.GetString(3);
                            var lastcheck = DateTime.FromBinary(reader.GetInt64(4));
                            var verified = reader.GetBoolean(5);
                            var counter = (int)reader.GetInt64(6);
                            var taken = DateTime.FromBinary(reader.GetInt64(7));
                            var meta = (int)reader.GetInt64(8);
                            var magnitude = reader.GetFloat(9);
                            var horizon = reader.GetString(10);
                            var viewed = (int)reader.GetInt64(11);
                            img = new Img(
                                name: name,
                                orientation: orientation,
                                lastview: lastview,
                                next: next,
                                lastcheck: lastcheck,
                                verified: verified,
                                counter: counter,
                                taken: taken,
                                meta: meta,
                                magnitude: magnitude,
                                horizon: horizon,
                                viewed: viewed
                            );
                        }
                    }
                }
            }

            return img;
        }

        public static string GetHash(string key)
        {
            string hash = null;
            lock (_lock) {
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append($"{AppConsts.AttributeHash}"); // 0
                sb.Append($" FROM {AppConsts.TableImages} ORDER BY {key} LIMIT 1");
                using (var sqlCommand = new SQLiteCommand(sb.ToString(), _sqlConnection))
                using (var reader = sqlCommand.ExecuteReader()) {
                    if (reader.Read()) {
                        hash = reader.GetString(0);
                    }
                }
            }

            return hash;
        }

        public static string GetForView()
        {
            string hash = null;
            lock (_lock) {
                var sb = new StringBuilder();
                // SELECT hash, lastview, counter FROM images WHERE counter = (SELECT MIN(Counter) FROM images) ORDER BY lastview DESC LIMIT 1 OFFSET 10
                /*
                sb.Append($"SELECT {AppConsts.AttributeHash}"); // 0
                sb.Append($" FROM {AppConsts.TableImages}");
                sb.Append($" WHERE {AppConsts.AttributeCounter} ="); 
                sb.Append($" (SELECT MIN({AppConsts.AttributeCounter}) FROM {AppConsts.TableImages})");
                sb.Append($" ORDER BY {AppConsts.AttributeLastView} DESC LIMIT 1 OFFSET 100");
                */

                sb.Append($"SELECT {AppConsts.AttributeHash}"); // 0
                sb.Append($" FROM {AppConsts.TableImages}");
                sb.Append($" WHERE {AppConsts.AttributeViewed} > 0");
                sb.Append($" ORDER BY {AppConsts.AttributeCounter}, {AppConsts.AttributeViewed}, {AppConsts.AttributeHash}");
                sb.Append($" LIMIT 1");

                /*
                sb.Append($"SELECT {AppConsts.AttributeHash}"); // 0
                sb.Append($" FROM {AppConsts.TableImages}");
                sb.Append($" ORDER BY {AppConsts.AttributeCounter}, {AppConsts.AttributeViewed}, {AppConsts.AttributeHash}");
                sb.Append($" LIMIT 1");
                */

                using (var sqlCommand = new SQLiteCommand(sb.ToString(), _sqlConnection))
                using (var reader = sqlCommand.ExecuteReader()) {
                    if (reader.Read()) {
                        hash = reader.GetString(0);
                    }
                }
            }

            return hash;
        }

        public static string GetForCheck()
        {
            string hash = null;
            lock (_lock) {
                var sb = new StringBuilder();
                // SELECT hash, lastview, counter FROM images WHERE counter = (SELECT MIN(Counter) FROM images) ORDER BY lastview DESC LIMIT 1 OFFSET 10
                /*
                sb.Append($"SELECT {AppC onsts.AttributeHash}"); // 0
                sb.Append($" FROM {AppConsts.TableImages}");
                sb.Append($" WHERE {AppConsts.AttributeCounter} ="); 
                sb.Append($" (SELECT MIN({AppConsts.AttributeCounter}) FROM {AppConsts.TableImages})");
                sb.Append($" ORDER BY {AppConsts.AttributeLastView} DESC LIMIT 1 OFFSET 100");
                */

                sb.Append($"SELECT {AppConsts.AttributeHash}"); // 0
                sb.Append($" FROM {AppConsts.TableImages}");
                sb.Append($" WHERE {AppConsts.AttributeViewed} > 0");
                sb.Append($" ORDER BY {AppConsts.AttributeCounter}, {AppConsts.AttributeViewed}, {AppConsts.AttributeHash}");
                sb.Append($" LIMIT 1");

                /*
                sb.Append($"SELECT {AppConsts.AttributeHash}"); // 0
                sb.Append($" FROM {AppConsts.TableImages}");
                sb.Append($" ORDER BY {AppConsts.AttributeCounter}, {AppConsts.AttributeViewed}, {AppConsts.AttributeHash}");
                sb.Append($" LIMIT 1");
                */

                using (var sqlCommand = new SQLiteCommand(sb.ToString(), _sqlConnection))
                using (var reader = sqlCommand.ExecuteReader()) {
                    if (reader.Read()) {
                        hash = reader.GetString(0);
                    }
                }
            }

            return hash;
        }

        public static DateTime GetMinimal(string key)
        {
            var dt = DateTime.Now;
            lock (_lock) {
                var sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append($"{key}"); // 0
                sb.Append($" FROM {AppConsts.TableImages} ORDER BY {key} LIMIT 1");
                using (var sqlCommand = new SQLiteCommand(sb.ToString(), _sqlConnection))
                using (var reader = sqlCommand.ExecuteReader()) {
                    if (reader.Read()) {
                        dt = DateTime.FromBinary(reader.GetInt64(0));
                    }
                }
            }

            return dt.AddSeconds(-1);
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
            }
        }

        /*
        public static void Populate(IProgress<string> progress)
        {
        }
        */
        
        public static void AddImg(string hash, float[] vector, Img img)
        {
            lock (_lock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    var sb = new StringBuilder();
                    sb.Append($"INSERT INTO {AppConsts.TableImages} (");
                    sb.Append($"{AppConsts.AttributeHash},");
                    sb.Append($"{AppConsts.AttributeName},");
                    sb.Append($"{AppConsts.AttributeVector},");
                    sb.Append($"{AppConsts.AttributeOrientation},");
                    sb.Append($"{AppConsts.AttributeLastView},");
                    sb.Append($"{AppConsts.AttributeNext},");
                    sb.Append($"{AppConsts.AttributeLastCheck},");
                    sb.Append($"{AppConsts.AttributeVerified},");
                    sb.Append($"{AppConsts.AttributeCounter},");
                    sb.Append($"{AppConsts.AttributeTaken},");
                    sb.Append($"{AppConsts.AttributeMeta},");
                    sb.Append($"{AppConsts.AttributeMagnitude},");
                    sb.Append($"{AppConsts.AttributeHorizon},");
                    sb.Append($"{AppConsts.AttributeViewed}");
                    sb.Append(") VALUES (");
                    sb.Append($"@{AppConsts.AttributeHash},");
                    sb.Append($"@{AppConsts.AttributeName},");
                    sb.Append($"@{AppConsts.AttributeVector},");
                    sb.Append($"@{AppConsts.AttributeOrientation},");
                    sb.Append($"@{AppConsts.AttributeLastView},");
                    sb.Append($"@{AppConsts.AttributeNext},");
                    sb.Append($"@{AppConsts.AttributeLastCheck},");
                    sb.Append($"@{AppConsts.AttributeVerified},");
                    sb.Append($"@{AppConsts.AttributeCounter},");
                    sb.Append($"@{AppConsts.AttributeTaken},");
                    sb.Append($"@{AppConsts.AttributeMeta},");
                    sb.Append($"@{AppConsts.AttributeMagnitude},");
                    sb.Append($"@{AppConsts.AttributeHorizon},");
                    sb.Append($"@{AppConsts.AttributeViewed}");
                    sb.Append(')');
                    sqlCommand.CommandText = sb.ToString();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeName}", img.Name);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", Helper.ArrayFromFloat(vector));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}", (int)img.Orientation);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", img.Next);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", img.LastCheck.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", img.Verified);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeCounter}", img.Counter);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeTaken}", img.Taken.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMeta}", img.Meta);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMagnitude}", img.Magnitude);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHorizon}", img.Horizon);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeViewed}", img.Viewed);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public static void SetVector(string hash, float[] vector, float magnitude)
        {
            lock (_lock) {
                using (var sqlCommand = _sqlConnection.CreateCommand()) {
                    sqlCommand.Connection = _sqlConnection;
                    sqlCommand.CommandText =
                        $"UPDATE {AppConsts.TableImages} SET {AppConsts.AttributeMagnitude} = @{AppConsts.AttributeMagnitude}, {AppConsts.AttributeVector} = @{AppConsts.AttributeVector} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMagnitude}", magnitude);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", Helper.ArrayFromFloat(vector));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public static void SetOrientation(string hash, RotateFlipType orientation)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeOrientation, (byte)orientation);
        }

        private static void SetLastView(string hash, DateTime lastview)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeLastView, lastview.Ticks);
        }

        public static void SetVerified(string hash)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeVerified, true);
        }

        public static void SetLastCheck(string hash, DateTime lastcheck)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeLastCheck, lastcheck.Ticks);
        }

        public static void SetNext(string hash, string next)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeNext, next);
        }

        public static void SetHorizon(string hash, string horizon)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeHorizon, horizon);
        }

        public static void SetCounter(string hash, int counter)
        {
            ImgUpdateProperty(hash, AppConsts.AttributeCounter, counter);
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
    }
}