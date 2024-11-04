using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace everlaster
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    static partial class StringExtensions
    {
        public static string Color(this string str, string color) => $"<color={color}>{str}</color>";
        public static string Color(this string str, Color color) => str.Color($"#{ColorUtility.ToHtmlStringRGB(color)}");

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
