using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;

static class FileUtils
{
    const string DATA_DIR = "Custom/PluginData/everlaster/PackageLicenseFilter";
    public const string PREFS_FILE = "preferences.json";
    public const string LICENSE_CACHE_FILE = "licensecache.json";
    public const string DISABLED_EXT = "disabled";

    public static IEnumerable<string> FindDirPaths(string rootPath, string dirName)
    {
        var result = new List<string>();
        var searchDirs = new Stack<string>();
        searchDirs.Push(FileManagerSecure.NormalizePath(rootPath));

        while(searchDirs.Count > 0)
        {
            string searchDir = searchDirs.Pop();
            foreach(string dir in FileManagerSecure.GetDirectories(searchDir))
            {
                string normalizedDir = FileManagerSecure.NormalizePath(dir);
                if(normalizedDir.Contains(".var:"))
                {
                    continue;
                }

                if(Utils.BaseName(normalizedDir) == dirName)
                {
                    result.Add(normalizedDir + "/");
                }
                else
                {
                    searchDirs.Push(normalizedDir);
                }
            }
        }

        return result;
    }

    public static IEnumerable<string> FindVarFilePaths(string addonPackagesLocation)
    {
        return FindFilePaths(addonPackagesLocation, "*.var");
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

    public static JSONClass ReadLicenseCacheJSON()
    {
        EnsureDataDirExists();
        return ReadJSON($"{DATA_DIR}/{LICENSE_CACHE_FILE}");
    }

    public static void WriteLicenseCacheJSON(JSONClass jc, UserActionCallback confirmCallback = null)
    {
        EnsureDataDirExists();
        WriteJSON(jc, $"{DATA_DIR}/{LICENSE_CACHE_FILE}", confirmCallback);
    }

    public static JSONClass ReadPrefsJSON()
    {
        EnsureDataDirExists();
        return ReadJSON($"{DATA_DIR}/{PREFS_FILE}");
    }

    public static void WritePrefsJSON(JSONClass jc, UserActionCallback confirmCallback = null)
    {
        EnsureDataDirExists();
        WriteJSON(jc, $"{DATA_DIR}/{PREFS_FILE}", confirmCallback);
    }

    public static JSONClass ReadJSON(string path)
    {
        JSONClass jc = null;
        try
        {
            if(FileManagerSecure.FileExists(path))
            {
                jc = JSON.Parse(FileManagerSecure.ReadAllText(path)).AsObject;
            }
        }
        catch(Exception e)
        {
            Debug.Log($"{nameof(FileUtils)}.{nameof(ReadJSON)}: {e}");
        }

        return jc;
    }

    public static void WriteJSON(JSONClass jc, string path, UserActionCallback confirmCallback = null)
    {
        FileManagerSecure.WriteAllText(path, jc.ToString(""), confirmCallback, null, null);
    }

    public static bool FileExists(string path)
    {
        return FileManagerSecure.FileExists(path);
    }

    public static void DeleteDisabledFile(string addonPackagePath)
    {
        FileManagerSecure.DeleteFile($"{addonPackagePath}.{DISABLED_EXT}");
    }

    public static void CreateDisabledFile(string addonPackagePath)
    {
        FileManagerSecure.WriteAllText($"{addonPackagePath}.{DISABLED_EXT}", string.Empty);
    }

    static void EnsureDataDirExists()
    {
        if(!FileManagerSecure.DirectoryExists(DATA_DIR))
        {
            FileManagerSecure.CreateDirectory(DATA_DIR);
        }
    }

    /* MVR.FileManagement FileManager */
    public static string RemovePackageFromPath(string path)
    {
        return Regex.Replace(Regex.Replace(path, ".*:/", string.Empty), ".*:\\\\", string.Empty);
    }
}
