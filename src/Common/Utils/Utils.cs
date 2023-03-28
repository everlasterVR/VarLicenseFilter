using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

static class Utils
{
    public static string BaseName(string path)
    {
        return path.Substring(path.LastIndexOf('/') + 1);
    }

    public static Transform DestroyLayout(Transform transform)
    {
        Object.Destroy(transform.GetComponent<LayoutElement>());
        return transform;
    }

    public static Regex NewRegex(string regexStr)
    {
        return new Regex(regexStr, RegexOptions.Compiled);
    }
}
