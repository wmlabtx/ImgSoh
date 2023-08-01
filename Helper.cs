using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;

namespace ImgSoh
{
    public static class Helper
    {
        public static string TimeIntervalToString(TimeSpan ts)
        {
            if (ts.TotalDays >= 2.0)
                return $"{ts.TotalDays:F0} days";

            if (ts.TotalDays >= 1.0)
                return $"{ts.TotalDays:F0} day";

            if (ts.TotalHours >= 2.0)
                return $"{ts.TotalHours:F0} hours";

            if (ts.TotalHours >= 1.0)
                return $"{ts.TotalHours:F0} hour";

            if (ts.TotalMinutes >= 2.0)
                return $"{ts.TotalMinutes:F0} minutes";

            if (ts.TotalMinutes >= 1.0)
                return $"{ts.TotalMinutes:F0} minute";

            return $"{ts.TotalSeconds:F0} seconds";
        }

        public static string SizeToString(long size)
        {
            var str = $"{size} b";
            if (size < 1024)
                return str;

            var kSize = (double)size / 1024;
            str = $"{kSize:F1} Kb";
            if (kSize < 1024) {
                return str;
            }

            kSize /= 1024;
            str = $"{kSize:F4} Mb";
            return str;
        }

        public static void CleanupDirectories(string startLocation, IProgress<string> progress)
        {
            foreach (var directory in Directory.GetDirectories(startLocation)) {
                Helper.CleanupDirectories(directory, progress);
                if (Directory.GetFiles(directory).Length != 0 || Directory.GetDirectories(directory).Length != 0) {
                    continue;
                }

                progress.Report($"{directory} deleting{AppConsts.CharEllipsis}");
                try {
                    Directory.Delete(directory, false);
                }
                catch (IOException) {
                }
            }
        }

        public static byte[] ArrayFrom16(ushort[] array)
        {
            var buffer = new byte[array.Length * sizeof(ushort)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            return buffer;
        }

        public static ushort[] ArrayTo16(byte[] buffer)
        {
            var array = new ushort[buffer.Length / sizeof(ushort)];
            Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
            return array;
        }

        public static byte[] ArrayFrom32(int[] array)
        {
            var buffer = new byte[array.Length * sizeof(int)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            return buffer;
        }

        public static int[] ArrayTo32(byte[] buffer)
        {
            var array = new int[buffer.Length / sizeof(int)];
            Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
            return array;
        }

        public static byte[] ArrayFrom64(ulong[] array)
        {
            var buffer = new byte[array.Length * sizeof(ulong)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            return buffer;
        }

        public static ulong[] ArrayTo64(byte[] buffer)
        {
            var array = new ulong[buffer.Length / sizeof(ulong)];
            Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
            return array;
        }

        public static byte[] ArrayFromFloat(float[] array)
        {
            var buffer = new byte[array.Length * sizeof(float)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            return buffer;
        }

        public static float[] ArrayToFloat(byte[] buffer)
        {
            var array = new float[buffer.Length / sizeof(float)];
            Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
            return array;
        }

        public static int GetMatch(byte[] x, byte[] y)
        {
            if (x == null || x.Length == 0 || y == null || y.Length == 0) {
                return 0;
            }

            var m = 0;
            var i = 0;
            var j = 0;
            while (i < x.Length && j < y.Length) {
                if (x[i] == y[j]) {
                    m++;
                    i++;
                    j++;
                }
                else {
                    if (x[i] < y[j]) {
                        i++;
                    }
                    else {
                        j++;
                    }
                }
            }

            return m;
        }

        public static System.Windows.Media.SolidColorBrush GetBrush(int id)
        {
            byte rByte, gByte, bByte;
            var array = BitConverter.GetBytes(id);
            using (var md5 = MD5.Create()) {
                var hashMD5 = md5.ComputeHash(array);
                rByte = (byte)(hashMD5[4] | 0x80);
                gByte = (byte)(hashMD5[7] | 0x80);
                bByte = (byte)(hashMD5[10] | 0x80);
            }

            var scb = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(rByte, gByte, bByte));
            return scb;
        }

        public static string GetShortDateTaken(DateTime dateTaken)
        {
            return $"{dateTaken.Year}:{dateTaken.Month:D2}:{dateTaken.Day:D2}";
        }

        public static byte RotateFlipTypeToByte(RotateFlipType rft)
        {
            switch (rft) {
                case RotateFlipType.RotateNoneFlipNone: 
                    return 0;
                case RotateFlipType.Rotate90FlipNone: 
                    return 1;
                case RotateFlipType.Rotate270FlipNone:
                    return 2;
                case RotateFlipType.Rotate180FlipNone:
                    return 3;
                default:
                    return 0;
            }
        }

        public static RotateFlipType ByteToRotateFlipType(byte b)
        {
            switch (b) {
                case 0:
                    return RotateFlipType.RotateNoneFlipNone;
                case 1:
                    return RotateFlipType.Rotate90FlipNone;
                case 2:
                    return RotateFlipType.Rotate270FlipNone;
                case 3:
                    return RotateFlipType.Rotate180FlipNone;
                default:
                    return RotateFlipType.RotateNoneFlipNone;
            }
        }

        public static string GetFileName(string folder, string hash)
        {
            return $"{AppConsts.PathHp}\\{folder[0]}\\{folder[1]}\\{hash}{AppConsts.MzxExtension}";
        }

        public static string GetShortFileName(string folder, string hash)
        {
            return $"{folder}\\{hash.Substring(0, 4)}.{hash.Substring(4, 4)}.{hash.Substring(8, 4)}";
        }

        public static string GetFolder()
        {
            var iFolder = AppVars.RandomNext(256);
            var folder = $"{iFolder:x2}";
            return folder;
        }
    }
}