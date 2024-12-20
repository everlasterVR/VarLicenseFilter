using System;
using System.Collections.Generic;
using System.Linq;
using MVR.FileManagementSecure;
using SimpleJSON;
using UnityEngine;

namespace everlaster
{
    static partial class FileUtils
    {
        const string DATA_DIR = "Custom/PluginData/everlaster/VarLicenseFilter";
        const string PREFS_FILE = "preferences.json";
        const string LICENSE_CACHE_FILE = "license_cache.json";
        const string SECONDARY_LICENSE_CACHE_FILE = "secondary_license_cache.json";
        const string ALWAYS_ENABLED_CACHE_FILE = "always_enabled_packages.txt";
        const string ALWAYS_DISABLED_CACHE_FILE = "always_disabled_packages.txt";
        const string TMP_ENABLED_FILE = "tmp_enabled_packages.txt";
        const string DISABLED_EXT = "disabled";

        public static string GetTmpEnabledFileFullPath() => $"{DATA_DIR}/{TMP_ENABLED_FILE}";

        public static IEnumerable<string> FindAddonPackagesInPluginDataDirPaths(string rootDir)
        {
            string rootPath = $@"{rootDir}\PluginData";
            var result = new List<string>();
            var searchDirs = new Stack<string>();
            searchDirs.Push(FileManagerSecure.NormalizePath(rootPath));

            while(searchDirs.Count > 0)
            {
                string[] dirs;
                try
                {
                    dirs = FileManagerSecure.GetDirectories(searchDirs.Pop());
                }
                catch(Exception e)
                {
                    Debug.Log("Error in FindDirPaths: " + e);
                    continue;
                }

                foreach(string dir in dirs)
                {
                    string normalizedDir = FileManagerSecure.NormalizePath(dir);
                    if(normalizedDir.Contains(".var:"))
                    {
                        continue;
                    }

                    if(normalizedDir.BaseName() == "AddonPackages")
                    {
                        result.Add(normalizedDir + "/");
                    }
                    else if(normalizedDir.CountOccurrences("PluginData") == 1)
                    {
                        searchDirs.Push(normalizedDir);
                    }
                }
            }

            return result;
        }

        public static IEnumerable<string> FindVarFilePaths(string addonPackagesLocation) =>
            FindFilePaths(addonPackagesLocation, "*.var");

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

        public static string NormalizePackagePath(string addonPackagesLocation, string packagePath) =>
            packagePath.Replace(addonPackagesLocation, "AddonPackages/");

        public static JSONClass ReadLicenseCacheJSON()
        {
            EnsureDirExists(DATA_DIR);
            return ReadJSON($"{DATA_DIR}/{LICENSE_CACHE_FILE}");
        }

        public static void WriteLicenseCacheJSON(JSONClass jc, UserActionCallback confirmCallback = null)
        {
            EnsureDirExists(DATA_DIR);
            WriteJSON(jc, $"{DATA_DIR}/{LICENSE_CACHE_FILE}", confirmCallback);
        }

        public static JSONClass ReadSecondaryLicenseCacheJSON()
        {
            EnsureDirExists(DATA_DIR);
            return ReadJSON($"{DATA_DIR}/{SECONDARY_LICENSE_CACHE_FILE}");
        }

        public static void WriteSecondaryLicenseCacheJSON(JSONClass jc, UserActionCallback confirmCallback = null)
        {
            EnsureDirExists(DATA_DIR);
            WriteJSON(jc, $"{DATA_DIR}/{SECONDARY_LICENSE_CACHE_FILE}", confirmCallback);
        }

        public static IEnumerable<string> ReadAlwaysEnabledCache()
        {
            EnsureDirExists(DATA_DIR);
            string text = ReadText($"{DATA_DIR}/{ALWAYS_ENABLED_CACHE_FILE}");
            return !string.IsNullOrEmpty(text.Trim()) ? text.Split('\n') : new string[0];
        }

        public static void WriteAlwaysEnabledCache(IEnumerable<string> set)
        {
            EnsureDirExists(DATA_DIR);
            FileManagerSecure.WriteAllText($"{DATA_DIR}/{ALWAYS_ENABLED_CACHE_FILE}", string.Join("\n", set.ToArray()));
        }

        public static IEnumerable<string> ReadAlwaysDisabledCache()
        {
            EnsureDirExists(DATA_DIR);
            string text = ReadText($"{DATA_DIR}/{ALWAYS_DISABLED_CACHE_FILE}");
            return !string.IsNullOrEmpty(text.Trim()) ? text.Split('\n') : new string[0];
        }

        public static void WriteAlwaysDisabledCache(IEnumerable<string> set)
        {
            EnsureDirExists(DATA_DIR);
            FileManagerSecure.WriteAllText($"{DATA_DIR}/{ALWAYS_DISABLED_CACHE_FILE}", string.Join("\n", set.ToArray()));
        }

        public static JSONClass ReadPrefsJSON()
        {
            EnsureDirExists(DATA_DIR);
            return ReadJSON($"{DATA_DIR}/{PREFS_FILE}");
        }

        public static void WritePrefsJSON(JSONClass jc, UserActionCallback confirmCallback = null)
        {
            EnsureDirExists(DATA_DIR);
            WriteJSON(jc, $"{DATA_DIR}/{PREFS_FILE}", confirmCallback);
        }

        public static IEnumerable<string> ReadTmpEnabledPackagesFile()
        {
            EnsureDirExists(DATA_DIR);
            return ReadText(GetTmpEnabledFileFullPath()).Split('\n');
        }

        public static void WriteTmpEnabledPackagesFile(string text)
        {
            EnsureDirExists(DATA_DIR);
            FileManagerSecure.WriteAllText(GetTmpEnabledFileFullPath(), text);
        }

        public static void DeleteTmpEnabledPackagesFile() => FileManagerSecure.DeleteFile(GetTmpEnabledFileFullPath());

        static string ReadText(string path) => FileExists(path) ? FileManagerSecure.ReadAllText(path) : "";

        static void WriteJSON(JSONClass jc, string path, UserActionCallback confirmCallback = null) =>
            FileManagerSecure.WriteAllText(path, jc.ToString(""), confirmCallback, null, null);

        public static bool DisabledFileExists(string packagePath) => FileExists($"{packagePath}.{DISABLED_EXT}");
        public static void DeleteDisabledFile(string packagePath) => DeleteFile($"{packagePath}.{DISABLED_EXT}");
        public static void CreateDisabledFile(string packagePath) => FileManagerSecure.WriteAllText($"{packagePath}.{DISABLED_EXT}", string.Empty);
    }
}
