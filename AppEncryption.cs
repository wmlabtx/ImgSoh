using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ImgSoh
{
    public static class AppEncryption
    {
        private const string PasswordSole = "{mzx}";
        private static readonly byte[] _aesIv = {
            0xE1, 0xD9, 0x94, 0xE6, 0xE6, 0x43, 0x39, 0x34,
            0x33, 0x0A, 0xCC, 0x9E, 0x7D, 0x66, 0x97, 0x16
        };

        private static Aes CreateAes(string password)
        {
            using (var hash256 = SHA256.Create()) {
                var passwordWithSole = string.Concat(password, PasswordSole);
                var passwordBuffer = Encoding.ASCII.GetBytes(passwordWithSole);
                var passwordKey256 = hash256.ComputeHash(passwordBuffer);
                var aes = Aes.Create();
                aes.KeySize = 256;
                aes.Key = passwordKey256;
                aes.BlockSize = 128;
                aes.IV = _aesIv;
                aes.Mode = CipherMode.CBC;
                return aes;
            }
        }

        public static byte[] Decrypt(byte[] array, string password)
        {
            var aes = CreateAes(password);
            try {
                using (var ms = new MemoryStream(array)) {
                    byte[] decoded;
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read)) {
                        var count = cs.Read(array, 0, array.Length);
                        decoded = new byte[count];
                        ms.Seek(0, SeekOrigin.Begin);
                        var _ = ms.Read(decoded, 0, count);
                    }

                    return decoded;
                }
            }
            catch (CryptographicException) {
                return null;
            }
            finally {
                aes.Dispose();
            }
        }

        public static byte[] Encrypt(byte[] array, string password)
        {
            var aes = CreateAes(password);
            try {
                using (var ms = new MemoryStream()) {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
                        cs.Write(array, 0, array.Length);
                    }

                    return ms.ToArray();
                }
            }
            finally {
                aes.Dispose();
            }
        }

        private static readonly byte[] _saltBytes = { 0xFF, 0x15, 0x20, 0xD5, 0x24, 0x1E, 0x12, 0xAA, 0xCC, 0xFF };
        private const int Iterations = 1000;

        public static byte[] DecryptDat(byte[] bytesToBeDecrypted, string password)
        {
            if (bytesToBeDecrypted == null || password == null) {
                return null;
            }

            byte[] decryptedBytes = null;

            try {
                using (var ms = new MemoryStream())
                using (var aes = new RijndaelManaged()) {
                    aes.KeySize = 256;
                    aes.BlockSize = 128;
                    var passwordBytes = Encoding.ASCII.GetBytes(password);
                    using (var key = new Rfc2898DeriveBytes(passwordBytes, _saltBytes, Iterations)) {
                        aes.Key = key.GetBytes(aes.KeySize / 8);
                        aes.IV = key.GetBytes(aes.BlockSize / 8);
                        aes.Mode = CipherMode.CBC;
                        using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write)) {
                            cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                            cs.Flush();
                        }

                        decryptedBytes = ms.ToArray();
                    }
                }
            }
            catch (CryptographicException) {
            }

            return decryptedBytes;
        }
    }
}
