using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
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

        public static void ImgUpdateProperty(string hash, string key, object val)
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
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static void DeleteImg(string hash)
        {
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        var transaction = _sqlConnection.BeginTransaction();
                        try {
                            sqlCommand.Connection = _sqlConnection;
                            sqlCommand.CommandText =
                                $"DELETE FROM {AppConsts.TableImages} WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}";
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                            sqlCommand.Transaction = transaction;
                            sqlCommand.ExecuteNonQuery();

                            sqlCommand.Connection = _sqlConnection;
                            sqlCommand.CommandText =
                                $"DELETE FROM {AppConsts.TablePairs} WHERE {AppConsts.AttributeId1} = @{hash} OR {AppConsts.AttributeId2} = @{hash}";
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.AddWithValue($"@{hash}", hash);
                            sqlCommand.Transaction = transaction;
                            sqlCommand.ExecuteNonQuery();

                            transaction.Commit();
                        }
                        catch {
                            try {
                                transaction.Rollback();
                            }
                            catch {
                                // ignored
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static int ImgCount()
        {
            int count;
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = $"SELECT COUNT(*) FROM {AppConsts.TableImages}";
                        count = (int)sqlCommand.ExecuteScalar();
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return count;
        }

        public static void AddPair(string id1, string id2, bool isFamily)
        {
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        var transaction = _sqlConnection.BeginTransaction();
                        try {
                            sqlCommand.Connection = _sqlConnection;
                            sqlCommand.CommandText =
                                $"DELETE FROM {AppConsts.TablePairs} WHERE ({AppConsts.AttributeId1} = @{id1} AND {AppConsts.AttributeId2} = @{id2}) OR ({AppConsts.AttributeId1} = @{id2} AND {AppConsts.AttributeId2} = @{id1})";
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.AddWithValue($"@{id1}", id1);
                            sqlCommand.Parameters.AddWithValue($"@{id2}", id2);
                            sqlCommand.Transaction = transaction;
                            sqlCommand.ExecuteNonQuery();

                            sqlCommand.Connection = _sqlConnection;
                            var sb = new StringBuilder();
                            sb.Append($"INSERT INTO {AppConsts.TablePairs} (");
                            sb.Append($"{AppConsts.AttributeId1}, ");
                            sb.Append($"{AppConsts.AttributeId2}, ");
                            sb.Append($"{AppConsts.AttributeIsFamily} ");
                            sb.Append(") VALUES (");
                            sb.Append($"@{AppConsts.AttributeId1}, ");
                            sb.Append($"@{AppConsts.AttributeId2}, ");
                            sb.Append($"@{AppConsts.AttributeIsFamily}");
                            sb.Append(')');
                            sqlCommand.CommandText = sb.ToString();
                            sqlCommand.Parameters.Clear();
                            sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeId1}", id1);
                            sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeId2}", id2);
                            sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeIsFamily}", isFamily);
                            sqlCommand.Transaction = transaction;
                            sqlCommand.ExecuteNonQuery();

                            transaction.Commit();
                        }
                        catch {
                            try {
                                transaction.Rollback();
                            }
                            catch {
                                // ignored
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static SortedList<string, byte[]> GetVectors()
        {
            var result = new SortedList<string, byte[]>();
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeHash}, "); // 0
                    sb.Append($"{AppConsts.AttributeVector} "); // 1
                    sb.Append($"FROM {AppConsts.TableImages}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                var hash = reader.GetString(0);
                                var vector = (byte[])reader[1];
                                result.Add(hash, vector);
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return result;
        }

        public static SortedList<string, DateTime> GetHashes()
        {
            var result = new SortedList<string, DateTime>();
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeHash}, "); // 0
                    sb.Append($"{AppConsts.AttributeLastView} "); // 1
                    sb.Append($"FROM {AppConsts.TableImages}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                var hash = reader.GetString(0);
                                var lastView = reader.GetDateTime(1);
                                result.Add(hash, lastView);
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return result;
        }

        public static SortedList<string, bool> GetPairs(string hash)
        {
            var result = new SortedList<string, bool>();
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeId1}, "); // 0
                    sb.Append($"{AppConsts.AttributeId2}, "); // 1
                    sb.Append($"{AppConsts.AttributeIsFamily} "); // 2
                    sb.Append($"FROM {AppConsts.TablePairs} ");
                    sb.Append($"WHERE {AppConsts.AttributeId1} = @{hash} OR {AppConsts.AttributeId2} = @{hash}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        sqlCommand.Parameters.AddWithValue($"@{hash}", hash);
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                var id1 = reader.GetString(0);
                                var id2 = reader.GetString(1);
                                var isFamily = reader.GetBoolean(2);
                                result.Add(id1.Equals(hash, StringComparison.Ordinal) ? id2 : id1, isFamily);
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return result;
        }

        public static List<Tuple<string, string>> GetPairs()
        {
            var result = new List<Tuple<string, string>>();
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeId1}, "); // 0
                    sb.Append($"{AppConsts.AttributeId2} "); // 1
                    sb.Append($"FROM {AppConsts.TablePairs}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                var id1 = reader.GetString(0);
                                var id2 = reader.GetString(1);
                                result.Add(Tuple.Create(id1, id2));
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return result;
        }

        public static void AddImg(
            string hash, 
            string folder, 
            byte[] vector, 
            DateTime lastView, 
            RotateFlipType orientation)
        {
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        var sb = new StringBuilder();
                        sb.Append($"INSERT INTO {AppConsts.TableImages} (");
                        sb.Append($"{AppConsts.AttributeHash}, ");
                        sb.Append($"{AppConsts.AttributeFolder}, ");
                        sb.Append($"{AppConsts.AttributeVector}, ");
                        sb.Append($"{AppConsts.AttributeLastView}, ");
                        sb.Append($"{AppConsts.AttributeOrientation}");
                        sb.Append(") VALUES (");
                        sb.Append($"@{AppConsts.AttributeHash}, ");
                        sb.Append($"@{AppConsts.AttributeFolder}, ");
                        sb.Append($"@{AppConsts.AttributeVector}, ");
                        sb.Append($"@{AppConsts.AttributeLastView}, ");
                        sb.Append($"@{AppConsts.AttributeOrientation}");
                        sb.Append(')');
                        sqlCommand.CommandText = sb.ToString();
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeFolder}", folder);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeVector}", vector);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeLastView}", lastView);
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeOrientation}", Helper.RotateFlipTypeToByte(orientation));
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }
        }

        public static bool TryGetImgFolder(string hash, out string folder)
        {
            folder = string.Empty;
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeFolder} "); // 0
                    sb.Append($"FROM {AppConsts.TableImages} ");
                    sb.Append($"WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                folder = reader.GetString(0);
                                return true;
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return false;
        }

        public static bool TryGetImgLastView(string hash, out DateTime lastView)
        {
            lastView = DateTime.MinValue;
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeLastView} "); // 0
                    sb.Append($"FROM {AppConsts.TableImages} ");
                    sb.Append($"WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                lastView = reader.GetDateTime(0);
                                return true;
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return false;
        }

        public static bool TryGetImgFolderOrientationLastView(string hash, out string folder, out RotateFlipType orientation, out DateTime lastView)
        {
            folder = string.Empty;
            orientation = RotateFlipType.RotateNoneFlipNone;
            lastView = DateTime.MinValue;
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeFolder}, "); // 0
                    sb.Append($"{AppConsts.AttributeOrientation}, "); // 1
                    sb.Append($"{AppConsts.AttributeLastView} "); // 2
                    sb.Append($"FROM {AppConsts.TableImages} ");
                    sb.Append($"WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                folder = reader.GetString(0);
                                orientation = Helper.ByteToRotateFlipType(reader.GetByte(1));
                                lastView = reader.GetDateTime(2);
                                return true;
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return false;
        }

        public static bool TryGetImgFolderLastView(string hash, out string folder, out DateTime lastView)
        {
            folder = string.Empty;
            lastView = DateTime.MinValue;
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeFolder}, "); // 0
                    sb.Append($"{AppConsts.AttributeLastView} "); // 1
                    sb.Append($"FROM {AppConsts.TableImages} ");
                    sb.Append($"WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                folder = reader.GetString(0);
                                lastView = reader.GetDateTime(1);
                                return true;
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return false;
        }

        public static bool TryGetImgVectorLastView(string hash, out string folder, out byte[] vector, out DateTime lastView)
        {
            folder = string.Empty;
            vector = null;
            lastView = DateTime.MinValue;
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sb = new StringBuilder();
                    sb.Append("SELECT ");
                    sb.Append($"{AppConsts.AttributeFolder}, "); // 0
                    sb.Append($"{AppConsts.AttributeVector}, "); // 1
                    sb.Append($"{AppConsts.AttributeLastView} "); // 2
                    sb.Append($"FROM {AppConsts.TableImages} ");
                    sb.Append($"WHERE {AppConsts.AttributeHash} = @{AppConsts.AttributeHash}");
                    var sqltext = sb.ToString();
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        sqlCommand.Parameters.AddWithValue($"@{AppConsts.AttributeHash}", hash);
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                folder = reader.GetString(0);
                                vector = (byte[])reader[1];
                                lastView = reader.GetDateTime(2);
                                return true;
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return false;
        }

        public static string GetNextView()
        {
            if (Monitor.TryEnter(_sqlLock, AppConsts.LockTimeout)) {
                try {
                    var sqltext = $"SELECT TOP(1) {AppConsts.AttributeHash} FROM {AppConsts.TableImages} ORDER BY {AppConsts.AttributeLastView}";
                    using (var sqlCommand = _sqlConnection.CreateCommand()) {
                        sqlCommand.Connection = _sqlConnection;
                        sqlCommand.CommandText = sqltext;
                        using (var reader = sqlCommand.ExecuteReader()) {
                            while (reader.Read()) {
                                var hash = reader.GetString(0);
                                return hash;
                            }
                        }
                    }
                }
                finally {
                    Monitor.Exit(_sqlLock);
                }
            }
            else {
                throw new Exception();
            }

            return null;
        }
    }
}
