using System;

namespace TumblThree.Applications.Extensions
{
    public static class StringExtension
    {
        public static string ToHash(this string text)
        {
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
            using (var md5 = System.Security.Cryptography.MD5.Create())
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
                byte[] hash = md5.ComputeHash(textData);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
