using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
static partial class StringExtensions
{
    public static string Bold(this string str)
    {
        return $"<b>{str}</b>";
    }

    public static string Italic(this string str)
    {
        return $"<i>{str}</i>";
    }

    public static string Size(this string str, int size)
    {
        return $"<size={size}>{str}</size>";
    }

    public static string Color(this string str, string color)
    {
        return $"<color={color}>{str}</color>";
    }

    public static string Color(this string str, Color color)
    {
        return str.Color($"#{ColorUtility.ToHtmlStringRGB(color)}");
    }

    public static string ReplaceLastOccurrence(this string str, string oldValue, string newValue)
    {
        int index = str.LastIndexOf(oldValue, StringComparison.Ordinal);
        return index == -1
            ? str
            : str.Remove(index, oldValue.Length).Insert(index, newValue);
    }

    public static int CountOccurrences(this string str, string substring)
    {
        int count = 0;
        int index = 0;

        while ((index = str.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }

        return count;
    }
}
