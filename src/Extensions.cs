using System;
using System.Diagnostics.CodeAnalysis;

namespace everlaster
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    static partial class StringExtensions
    {
        public static int CountOccurrences(this string str, string substring)
        {
            int count = 0;
            int index = 0;

            while((index = str.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += substring.Length;
            }

            return count;
        }

        public static string BaseName(this string path) => path.Substring(path.LastIndexOf('/') + 1);
    }
}
