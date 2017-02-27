using System;

namespace My.Extensions
{
    static class StringExtensions
    {
        public static string Left(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            maxLength = Math.Abs(maxLength);

            return (value.Length <= maxLength
                   ? value
                   : value.Substring(0, maxLength)
                   );
        }

        public static string Right(this string value, int length)
        {
            return value.Substring(value.Length - length);
        }

        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ?
                   value :
                   value.Substring(0, maxChars) + " ..";
        }
    }
}