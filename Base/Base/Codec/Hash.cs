using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Base.Codec
{
    class Hash
    {
        /// <summary>
        /// generate md5 code of code|bytes
        /// </summary>
        public static string MD5(string code)
        {
            return MD5(Encoding.UTF8.GetBytes(code));
        }
        public static string MD5(byte[] bytes)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] res = md5.ComputeHash(bytes);
            StringBuilder builder = new StringBuilder();
            foreach (byte data in res)
                builder.Append(data.ToString("x2"));
            return builder.ToString();
        }
        /// <summary>
        /// generate sha1 code of code|bytes
        /// </summary>
        public static string Sha1(string code)
        {
            return Sha1(Encoding.UTF8.GetBytes(code));
        }
        public static string Sha1(byte[] bytes)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] res = sha1.ComputeHash(bytes);
            StringBuilder builder = new StringBuilder();
            foreach (byte data in res)
                builder.Append(data.ToString("x2"));
            return builder.ToString();
        }
        /// <summary>
        /// generate RS Hash value
        /// </summary>
        public static UInt32 RSHash(string code)
        {
            return RSHash(Encoding.UTF8.GetBytes(code));
        }
        public static UInt32 RSHash(byte[] bytes)
        {
            UInt32 a = 378551;
            UInt32 b = 63689;
            UInt32 hash = 0;

            foreach (byte data in bytes)
            {
                hash = hash * a + data;
                a *= b;
            }

            return hash & 0x7fffffff;
        }
        /// <summary>
        /// generate BKDR Hash value
        /// </summary>
        public static UInt32 BKDRHash(string code)
        {
            return BKDRHash(Encoding.UTF8.GetBytes(code));
        }
        public static UInt32 BKDRHash(byte[] bytes)
        {
            UInt32 seed = 131313;
            UInt32 hash = 0;

            foreach (byte data in bytes)
                hash = hash * seed + data;

            return hash & 0x7fffffff;
        }
        /// <summary>
        /// generate JS Hash value
        /// </summary>
        public static UInt32 JSHash(string code)
        {
            return JSHash(Encoding.UTF8.GetBytes(code));
        }
        public static UInt32 JSHash(byte[] bytes)
        {
            UInt32 hash = 1315423911;

            foreach (byte data in bytes)
                hash ^= ((hash << 5) + data + (hash >> 2));

            return hash & 0x7fffffff;
        }
    }
}
