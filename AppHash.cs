﻿using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ImgSoh
{
    public static class AppHash
    {
        private static readonly MD5 _md5 = MD5.Create();

        private static string GetHexString(IReadOnlyCollection<byte> bytes)
        {
            var sb = new StringBuilder(bytes.Count * 2);
            foreach (var b in bytes) {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        public static string GetHash(byte[] data)
        {
            var buffer = _md5.ComputeHash(data);
            var hash = GetHexString(buffer);
            return hash;
        }

        public static string GetFamily()
        {
            var buffer = AppVars.RandomBuffer(3);
            return GetHexString(buffer).Substring(0, 5);
        }
    }
}
