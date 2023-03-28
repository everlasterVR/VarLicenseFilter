using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;

static class FileUtils
{
    public static IEnumerable<string> FindVarFilePaths()
    {
        return FindFilePaths("AddonPackages", "*.var");
    }

    static IEnumerable<string> FindFilePaths(string rootPath, string pattern)
    {
        var result = new List<string>();
        var searchDirs = new Stack<string>();
        searchDirs.Push(FileManagerSecure.NormalizePath(rootPath));

        while(searchDirs.Count > 0)
        {
            string searchDir = searchDirs.Pop();
            foreach(string file in FileManagerSecure.GetFiles(searchDir, pattern))
            {
                result.Add(FileManagerSecure.NormalizePath(file));
            }

            foreach(string dir in FileManagerSecure.GetDirectories(searchDir))
            {
                string normalizedDir = FileManagerSecure.NormalizePath(dir);
                if(normalizedDir.Contains(".var:"))
                {
                    continue;
                }

                searchDirs.Push(normalizedDir);
            }
        }

        return result;
    }

    public static JSONClass ReadJSON(string path)
    {
        try
        {
            return SuperController.singleton.LoadJSON(path).AsObject;
        }
        catch(Exception e)
        {
            Debug.Log($"{nameof(FileUtils)}.{nameof(ReadJSON)}: {e}");
            return new JSONClass();
        }
    }

    public static void WriteJSON(JSONClass jc, string path)
    {
        FileManagerSecure.WriteAllText(path, jc.ToString(""));
    }

    /* MVR.FileManagement FileManager */
    public static string RemovePackageFromPath(string path)
    {
        return Regex.Replace(Regex.Replace(path, ".*:/", string.Empty), ".*:\\\\", string.Empty);
    }
}
