using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;

static class FileUtils
{
    const string DATA_DIR = "Custom/PluginData/everlaster/PackageLicenseFilter";
    const string PREFS_FILE = "preferences.json";
    const string LICENSE_CACHE_FILE = "license_cache.json";
    const string ALWAYS_ENABLED_CACHE_FILE = "always_enabled_packages.txt";
    const string ALWAYS_DISABLED_CACHE_FILE = "always_disabled_packages.txt";
    const string TMP_ENABLED_FILE = "tmp_enabled_packages.txt";
    const string DISABLED_EXT = "disabled";

    public static string GetTmpEnabledFileFullPath()
    {
        return $"{DATA_DIR}/{TMP_ENABLED_FILE}";
    }

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

    public static string NormalizePackagePath(string addonPackagesLocation, string packagePath)
    {
        return packagePath.Replace(addonPackagesLocation, "AddonPackages/");
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

    public static IEnumerable<string> ReadAlwaysEnabledCache()
    {
        EnsureDataDirExists();
        string text = ReadText($"{DATA_DIR}/{ALWAYS_ENABLED_CACHE_FILE}");
        return !string.IsNullOrEmpty(text.Trim()) ? text.Split('\n') : new string[0];
    }

    public static void WriteAlwaysEnabledCache(IEnumerable<string> set)
    {
        EnsureDataDirExists();
        FileManagerSecure.WriteAllText($"{DATA_DIR}/{ALWAYS_ENABLED_CACHE_FILE}", string.Join("\n", set.ToArray()));
    }

    public static IEnumerable<string> ReadAlwaysDisabledCache()
    {
        EnsureDataDirExists();
        string text = ReadText($"{DATA_DIR}/{ALWAYS_DISABLED_CACHE_FILE}");
        return !string.IsNullOrEmpty(text.Trim()) ? text.Split('\n') : new string[0];
    }

    public static void WriteAlwaysDisabledCache(IEnumerable<string> set)
    {
        EnsureDataDirExists();
        FileManagerSecure.WriteAllText($"{DATA_DIR}/{ALWAYS_DISABLED_CACHE_FILE}", string.Join("\n", set.ToArray()));
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

    public static IEnumerable<string> ReadTmpEnabledPackagesFile()
    {
        EnsureDataDirExists();
        return ReadText(GetTmpEnabledFileFullPath()).Split('\n');

    }

    public static void WriteTmpEnabledPackagesFile(string text)
    {
        EnsureDataDirExists();
        FileManagerSecure.WriteAllText(GetTmpEnabledFileFullPath(), text);
    }

    public static void DeleteTmpEnabledPackagesFile()
    {
        FileManagerSecure.DeleteFile(GetTmpEnabledFileFullPath());
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

    static string ReadText(string path)
    {
        return FileManagerSecure.FileExists(path) ? FileManagerSecure.ReadAllText(path) : "";
    }

    public static void WriteJSON(JSONClass jc, string path, UserActionCallback confirmCallback = null)
    {
        FileManagerSecure.WriteAllText(path, jc.ToString(""), confirmCallback, null, null);
    }

    public static bool FileExists(string path)
    {
        return FileManagerSecure.FileExists(path);
    }

    public static bool DisabledFileExists(string packagePath)
    {
        return FileExists($"{packagePath}.{DISABLED_EXT}");
    }

    public static void DeleteDisabledFile(string packagePath)
    {
        Debug.Log($"deleting disabled file: {packagePath}.{DISABLED_EXT}");
        FileManagerSecure.DeleteFile($"{packagePath}.{DISABLED_EXT}");
    }

    public static void CreateDisabledFile(string packagePath)
    {
        FileManagerSecure.WriteAllText($"{packagePath}.{DISABLED_EXT}", string.Empty);
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
