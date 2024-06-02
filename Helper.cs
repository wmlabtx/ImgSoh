using System;
using System.Drawing;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Windows.Shapes;

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

        public static string GetFileName(string path, string hash, string ext)
        {
            return $"{AppConsts.PathHp}\\{path}\\{hash}.{ext}";
        }

        public static string GetShortFileName(string path, string hash)
        {
            return $"{path}\\{hash.Substring(0, 4)}.{hash.Substring(4, 4)}.{hash.Substring(8, 4)}";
        }

        public static string GetRandomPath()
        {
            var iFolder = AppVars.RandomNext(256);
            var folder = $"{iFolder:x2}";
            return $"-\\{folder[0]}\\{folder[1]}";
        }

        public static string GetRadius(string hash, float distance)
        {
            var rounded = (int)Math.Round(distance * 10000);
            rounded = Math.Max(0, Math.Min(9999, rounded));
            var radius = $"{rounded:D4}{hash}";
            return radius;
        }

        public static byte[] GetRawString(string hash, int pad)
        {
            var raw = Encoding.ASCII.GetBytes(hash.PadLeft(pad));
            if (raw.Length != pad) {
                throw new Exception("wrong raw.Length");
            }

            return raw;
        }

        public static byte[] GetRawDateTime(DateTime dt)
        {
            return BitConverter.GetBytes(dt.Ticks);
        }

        public static byte[] GetRawBool(bool flag)
        {
            return BitConverter.GetBytes(flag);
        }

        public static byte[] GetRawInt(int counter)
        {
            return BitConverter.GetBytes(counter);
        }

        public static byte[] GetRawVector(float[] vector)
        {
            var array = new byte[AppConsts.VectorLength * sizeof(float)];
            Buffer.BlockCopy(vector, 0, array, 0, array.Length);
            return array;
        }

        public static byte[] GetRawOrientation(RotateFlipType orientation)
        {
            return new[] { (byte)orientation };
        }

        public static string SetRawString(byte[] raw)
        {
            return Encoding.ASCII.GetString(raw).Trim();
        }

        public static DateTime SetRawDateTime(byte[] raw)
        {
            return DateTime.FromBinary(BitConverter.ToInt64(raw, 0));
        }

        public static bool SetRawBool(byte[] raw)
        {
            return BitConverter.ToBoolean(raw, 0);
        }

        public static int SetRawInt(byte[] raw)
        {
            return BitConverter.ToInt32(raw, 0);
        }

        public static float[] SetRawVector(byte[] raw)
        {
            var array = new float[AppConsts.VectorLength];
            Buffer.BlockCopy(raw, 0, array, 0, raw.Length);
            return array;
        }

        public static RotateFlipType SetRawOrientation(byte[] raw)
        {
            return (RotateFlipType)raw[0];
        }
    }
}