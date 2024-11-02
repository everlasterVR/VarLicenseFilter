using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace everlaster
{
    static partial class Utils
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
    }
}

