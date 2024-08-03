using System;
using System.Drawing;
using System.Data.SQLite;
using System.Text;

namespace ImgSoh
{
    public static class AppDatabase
    {
        private static readonly object _lock = new object();
        private static SQLiteConnection _sqlConnection;

        public static void LoadImages(string filedatabase, IProgress<string> progress)
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
                sb.Append($"{AppConsts.AttributeOrientation},"); // 3
                sb.Append($"{AppConsts.AttributeLastView},"); // 4
                sb.Append($"{AppConsts.AttributeNext},"); // 5
                sb.Append($"{AppConsts.AttributeLastCheck},"); // 6
                sb.Append($"{AppConsts.AttributeVerified},"); // 7
                sb.Append($"{AppConsts.AttributeCounter},"); // 8
                sb.Append($"{AppConsts.AttributeTaken},"); // 9
                sb.Append($"{AppConsts.AttributeMeta},"); // 10
                sb.Append($"{AppConsts.AttributeFamily},"); // 11
                sb.Append($"{AppConsts.AttributeMagnitude},"); // 12
                sb.Append($"{AppConsts.AttributeHorizon},"); // 13
                sb.Append($"{AppConsts.AttributeRank},"); // 14
                sb.Append($"{AppConsts.AttributeViewed} "); // 15
                sb.Append($"FROM {AppConsts.TableImages};");
                using (var command = new SQLiteCommand(sb.ToString(), _sqlConnection))
                using (var reader = command.ExecuteReader()) {
                    if (!reader.HasRows) {
                        return;
                    }

                    var dtn = DateTime.Now;
                    while (reader.Read()) {
                        var hash = reader.GetString(0);
                        var name = reader.GetString(1);
                        var vector = Helper.ArrayToFloat((byte[])reader[2]);
                        var orientation = (RotateFlipType)Enum.Parse(typeof(RotateFlipType), reader.GetInt64(3).ToString());
                        var lastview = DateTime.FromBinary(reader.GetInt64(4));
                        var next = reader.GetString(5);
                        var lastcheck = DateTime.FromBinary(reader.GetInt64(6));
                        var verified = reader.GetBoolean(7);
                        var counter = (int)reader.GetInt64(8);
                        var taken = DateTime.FromBinary(reader.GetInt64(9));
                        var meta = (int)reader.GetInt64(10);
                        var family = reader.GetString(11);
                        var magnitude = reader.GetFloat(12);
                        var horizon = reader.GetString(13);
                        var rank = reader.GetInt64(14);
                        var viewed = (int)reader.GetInt64(15);
                        var img = new Img(
                            hash: hash,
                            name: name,
                            vector: vector,
                            orientation: orientation,
                            lastview: lastview,
                            next: next,
                            lastcheck: lastcheck,
                            verified: verified,
                            counter: counter,
                            taken: taken,
                            meta: meta,
                            family: family,
                            magnitude: magnitude,
                            horizon: horizon,
                            rank: rank,
                            viewed: viewed
                        );

                        AppImgs.Add(img);
                        if (!(DateTime.Now.Subtract(dtn).TotalMilliseconds > AppConsts.TimeLapse)) {
                            continue;
                        }

                        dtn = DateTime.Now;
                        var count = AppImgs.Count();
                        progress?.Report($"Loading images ({count}){AppConsts.CharEllipsis}");
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
        
        public static void AddImg(Img img)
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
                    sb.Append($"{AppConsts.AttributeFamily},");
                    sb.Append($"{AppConsts.AttributeMagnitude},");
                    sb.Append($"{AppConsts.AttributeHorizon},");
                    sb.Append($"{AppConsts.AttributeRank},");
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
                    sb.Append($"@{AppConsts.AttributeFamily},");
                    sb.Append($"@{AppConsts.AttributeMagnitude},");
                    sb.Append($"@{AppConsts.AttributeHorizon},");
                    sb.Append($"@{AppConsts.AttributeRank},");
                    sb.Append($"@{AppConsts.AttributeViewed}");
                    sb.Append(')');
                    sqlCommand.CommandText = sb.ToString();
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", img.Hash);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeName}", img.Name);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", Helper.ArrayFromFloat(img.GetVector()));
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}", (int)img.Orientation);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", img.LastView.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeNext}", img.Next);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastCheck}", img.LastCheck.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVerified}", img.Verified);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeCounter}", img.Counter);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeTaken}", img.Taken.Ticks);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMeta}", img.Meta);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFamily}", img.Family);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeMagnitude}", img.Magnitude);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHorizon}", img.Horizon);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeRank}", img.Rank);
                    sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeViewed}", img.Viewed);
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        public static void SetVector(string hash, float[] vector, float magnitude)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.SetVector(vector);
                img.Magnitude = magnitude;
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
        }

        public static void SetOrientation(string hash, RotateFlipType orientation)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.Orientation = orientation;
                ImgUpdateProperty(hash, AppConsts.AttributeOrientation, (byte)orientation);
            }
        }

        public static void SetLastView(string hash, DateTime lastview)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.LastView = DateTime.Now;
                ImgUpdateProperty(hash, AppConsts.AttributeLastView, lastview.Ticks);
            }
        }

        public static void SetVerified(string hash)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.Verified = true;
                ImgUpdateProperty(hash, AppConsts.AttributeVerified, true);
            }
        }

        public static void SetLastCheck(string hash)
        {
            var lastcheck = DateTime.Now;
            SetLastCheck(hash, lastcheck);
        }

        private static void SetLastCheck(string hash, DateTime lastcheck)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.LastCheck = lastcheck;
                ImgUpdateProperty(hash, AppConsts.AttributeLastCheck, lastcheck.Ticks);
            }
        }

        public static void SetNext(string hash, string next)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.Next = next;
                ImgUpdateProperty(hash, AppConsts.AttributeNext, next);
            }
        }

        public static void SetHorizon(string hash)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.Horizon = img.Next;
                ImgUpdateProperty(hash, AppConsts.AttributeHorizon, img.Horizon);
            }
        }

        public static void SetCounter(string hash, int counter)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.Counter = counter;
                ImgUpdateProperty(hash, AppConsts.AttributeCounter, counter);
            }
        }

        public static void SetFamily(string hash, string family)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.Family = family;
                ImgUpdateProperty(hash, AppConsts.AttributeFamily, family);
            }
        }

        public static void SetRank(string hash, long rank)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.Rank = rank;
                ImgUpdateProperty(hash, AppConsts.AttributeRank, rank);
            }
        }

        public static void SetViewed(string hash, int viewed)
        {
            if (AppImgs.TryGetImg(hash, out var img)) {
                img.Viewed = viewed;
                ImgUpdateProperty(hash, AppConsts.AttributeViewed, viewed + 1);
            }
        }
    }
}