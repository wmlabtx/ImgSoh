using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ImgSoh
{
    public static class FileHelper
    {
        private const string AllowedChars = "0123456789abcdefghijkmnprstuvxyz"; // 36-4=32 l-o-w-q

        public static string GetHash(byte[] data)
        {
            var sb = new StringBuilder();
            using (var sha256 = SHA256.Create()) {
                var raw = sha256.ComputeHash(data);
                for (var i = 1; i <= 12; i++) {
                    var c = AllowedChars[raw[i] & 0x1f];
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static byte[] ReadEncryptedFile(string filename)
        {
            var encryptedArray = File.ReadAllBytes(filename);
            var password = Path.GetFileNameWithoutExtension(filename);
            var data = EncryptionHelper.Decrypt(encryptedArray, password);
            return data;
        }

        public static byte[] ReadFile(string filename)
        {
            var data = File.ReadAllBytes(filename);
            return data;
        }

        public static void WriteFile(string filename, byte[] data)
        {
            var directory = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filename, data);
        }

        public static void WriteEncryptedFile(string filename, byte[] data)
        {
            var directory = Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }

            var password = Path.GetFileNameWithoutExtension(filename);
            var encryptedData = EncryptionHelper.Encrypt(data, password);
            File.WriteAllBytes(filename, encryptedData);
        }

        public static void DeleteToRecycleBin(string filename, string bin)
        {
            try
            {
                if (!File.Exists(filename)) {
                    return;
                }

                var now = DateTime.Now;
                File.SetAttributes(filename, FileAttributes.Normal);
                var name = Path.GetFileNameWithoutExtension(filename);
                var extension = Path.GetExtension(filename);
                string deletedFilename;
                var counter = 0;
                do {
                    deletedFilename = counter == 0 ? 
                        $"{bin}\\{now.Year}-{now.Month:D2}-{now.Day:D2}\\{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.{name}{extension}" : 
                        $"{bin}\\{now.Year}-{now.Month:D2}-{now.Day:D2}\\{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.{name}({counter}){extension}";

                    counter++;
                }
                while (File.Exists(deletedFilename));
                var directory = Path.GetDirectoryName(deletedFilename);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                File.Move(filename, deletedFilename);
            }
            catch (UnauthorizedAccessException) {
            }
            catch (IOException) {
            }
        }

        public static void MoveCorruptedFile(string filename, string bin)
        {
            var badName = Path.GetFileName(filename);
            var badFilename = $"{bin}\\{badName}";
            if (badFilename.Equals(filename, StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            if (File.Exists(badFilename)) {
                DeleteToRecycleBin(badFilename, bin);
            }

            File.Move(filename, badFilename);
        }
    }
}
