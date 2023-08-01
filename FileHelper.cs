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
    }
}
