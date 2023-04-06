#define ENV_DEVELOPMENT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using UnityEngine;

sealed class VarLicenseFilter : ScriptBase
{
    /* Public static access point to plugin instance. */
    public const string VERSION = "0.0.0";
    public static bool envIsDevelopment { get; private set; }
    public static VarLicenseFilter script { get; private set; }

    public override bool ShouldIgnore()
    {
        return false;
    }

#region *** Init ***

    public static bool IsInitialized()
    {
        return script.initialized == true;
    }

    public override void Init()
    {
        #if ENV_DEVELOPMENT
        {
            envIsDevelopment = true;
        }
        #else
        {
            envIsDevelopment = false;
        }
        #endif
        script = this;

        try
        {
            /* Used to store version in save JSON and communicate version to other plugin instances */
            this.NewJSONStorableString(Constant.VERSION, VERSION);

            if(containingAtom.FindStorablesByRegexMatch(Utils.NewRegex($@"^plugin#\d+_{nameof(VarLicenseFilter)}")).Count > 0)
            {
                FailInitWithMessage($"An instance of {nameof(VarLicenseFilter)} is already added.");
                return;
            }

            StartCoroutine(InitCo());
        }
        catch(Exception e)
        {
            initialized = false;
            Loggr.Error($"Init: {e}");
        }
    }

    Dictionary<string, string> _packageLicenseCache;
    Dictionary<string, SecondaryLicenseCacheObject> _packageSecondaryLicenseCache;
    HashSet<string> _alwaysEnabledPackages;
    HashSet<string> _alwaysDisabledPackages;
    List<VarPackage> _varPackages;
    public readonly Dictionary<string, License> licenses = new Dictionary<string, License>
    {
        { License.FC.name, License.FC },
        { License.CC_BY.name, License.CC_BY },
        { License.CC_BY_SA.name, License.CC_BY_SA },
        { License.CC_BY_ND.name, License.CC_BY_ND },
        { License.CC_BY_NC.name, License.CC_BY_NC },
        { License.CC_BY_NC_SA.name, License.CC_BY_NC_SA },
        { License.CC_BY_NC_ND.name, License.CC_BY_NC_ND },
        { License.PC.name, License.PC },
        { License.PC_EA.name, License.PC_EA },
        { License.Questionable.name, License.Questionable },
    };

    public List<string> addonPackagesDirPaths { get; private set; }
    public JSONStorableString addonPackagesLocationJss { get; private set; }
    public JSONStorableAction saveAndContinueAction { get; private set; }
    public JSONStorableString filterInfoJss { get; private set; }
    public JSONStorableBool alwaysEnableSelectedJsb { get; private set; }
    public JSONStorableBool alwaysDisableSelectedJsb { get; private set; }
    public JSONStorableString alwaysEnabledListInfoJss { get; private set; }
    public JSONStorableString alwaysDisabledListInfoJss { get; private set; }
    public JSONStorableAction applyFilterAction { get; private set; }
    public JSONStorableAction undoRunFiltersAction { get; private set; }
    public JSONStorableAction fixAndRestartAction { get; private set; }
    public JSONStorableAction restartVamAction { get; private set; }
    public JSONStorableBool alwaysEnableDefaultSessionPluginsJsb { get; private set; }
    public JSONStorableStringChooser packageJssc { get; private set; }

    VarPackage _selectedPackage;
    bool _applyLicenseFilter;

    Bindings bindings { get; set; }

    IWindow _mainWindow;
    IWindow _setupWindow;

    IEnumerator InitCo()
    {
        yield return new WaitForEndOfFrame();
        while(SuperController.singleton.isLoading)
        {
            yield return null;
        }

        if(initialized == false)
        {
            yield break;
        }

        try
        {
            InitBindings(); // Might already be setup in OnBindingsListRequested.
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);
            initialized = true;
        }
        catch(Exception e)
        {
            FailInitWithError($"Init error: {e}");
        }
    }

    bool _lateInitDone;

    protected override Action OnUIEnabled()
    {
        return () =>
        {
            if(_lateInitDone)
            {
                if(!string.IsNullOrEmpty(addonPackagesLocationJss.val))
                {
                    bool syncNeeded = RefreshSessionPluginPackages();
                    if(syncNeeded)
                    {
                        UpdateAlwaysEnabledListInfoText();
                        UpdateAlwaysDisabledListInfoText();
                        SyncPackageStatuses();
                    }
                }

                return;
            }

            SetupStorables();
            ReadUserPreferencesFromFile();
            ReadLicenseCacheFromFile();
            ReadSecondaryLicenseCacheFromFile();
            _alwaysEnabledPackages = new HashSet<string>(FileUtils.ReadAlwaysEnabledCache());
            _alwaysDisabledPackages = new HashSet<string>(FileUtils.ReadAlwaysDisabledCache());
            _mainWindow = new MainWindow();
            _setupWindow = new SetupWindow();
            if(string.IsNullOrEmpty(addonPackagesLocationJss.val))
            {
                FindAddonPackagesDirPaths();
                _setupWindow.Rebuild();
            }
            else
            {
                InitPackages();
                _mainWindow.Rebuild();
            }

            _lateInitDone = true;
        };
    }

    /* In case session plugin defaults were changed since Init */
    bool RefreshSessionPluginPackages()
    {
        _defaultSessionPluginPackages = FindPackageFilenamesFromDefaultSessionPluginsJson();
        bool syncNeeded = false;
        foreach(string filename in _defaultSessionPluginPackages)
        {
            var package = _varPackages.FirstOrDefault(item => item.filename == filename);
            if(package == null)
            {
                Debug.Log($"Package {filename} not found?");
                continue;
            }

            package.isDefaultSessionPluginPackage = true;

            if(alwaysEnableDefaultSessionPluginsJsb.val)
            {
                syncNeeded = true;
                _alwaysEnabledPackages.Add(package.filename);
                _alwaysDisabledPackages.Remove(package.filename);
                package.forceEnabled = true;
                package.forceDisabled = false;

                if(package == _selectedPackage)
                {
                    alwaysEnableSelectedJsb.valNoCallback = package.forceEnabled;
                    alwaysDisableSelectedJsb.valNoCallback = package.forceDisabled;
                }
            }
        }

        return syncNeeded;
    }

    void FindAddonPackagesDirPaths()
    {
        addonPackagesDirPaths = new List<string>();
        addonPackagesDirPaths.AddRange(FileUtils.FindDirPaths(@"Custom\PluginData", "AddonPackages"));
        addonPackagesDirPaths.AddRange(FileUtils.FindDirPaths(@"Saves\PluginData", "AddonPackages"));
        // (string _) => ((SetupWindow) _setupWindow).Refresh()
    }

    void SetupStorables()
    {
        addonPackagesLocationJss = new JSONStorableString("AddonPackages location", "");
        saveAndContinueAction = new JSONStorableAction("Save selected location and continue", SaveAndContinue);
        filterInfoJss = new JSONStorableString("Filter info", "");
        alwaysEnableSelectedJsb = new JSONStorableBool("Always enable selected package", false, OnToggleAlwaysEnabled);
        alwaysDisableSelectedJsb = new JSONStorableBool("Always disable selected package", false, OnToggleAlwaysDisabled);
        alwaysEnabledListInfoJss = new JSONStorableString("Always enabled list info", "");
        alwaysDisabledListInfoJss = new JSONStorableString("Always disabled list info", "");
        applyFilterAction = new JSONStorableAction("Run filters and preview result", () =>
        {
            _applyLicenseFilter = true;
            SyncPackageStatuses();
        });
        undoRunFiltersAction = new JSONStorableAction("Undo run filters", () =>
        {
            _applyLicenseFilter = false;
            SyncPackageStatuses();
        });
        fixAndRestartAction = new JSONStorableAction("Fix cache and restart VAM", () =>
        {
            foreach(string path in _fixablePackagePaths)
            {
                FileUtils.DeleteDisabledFile(path);
            }

            FileUtils.WriteTmpEnabledPackagesFile(string.Join("\n", _fixablePackagePaths.ToArray()));
            RestartVAM();
        });
        restartVamAction = new JSONStorableAction("Save changes and restart VAM", () =>
        {
            FileUtils.WriteAlwaysEnabledCache(_alwaysEnabledPackages);
            FileUtils.WriteAlwaysDisabledCache(_alwaysDisabledPackages);

            foreach(var package in _varPackages)
            {
                if(!package.changed)
                {
                    continue;
                }

                if(package.enabled)
                {
                    FileUtils.DeleteDisabledFile(package.path);
                }
                else
                {
                    FileUtils.CreateDisabledFile(package.path);
                }
            }

            RestartVAM();
        });
        alwaysEnableDefaultSessionPluginsJsb = new JSONStorableBool(
            "Always enable default session plugin packages",
            true,
            OnToggleAlwaysEnableDefaultSessionPlugins
        );
        packageJssc = new JSONStorableStringChooser("Package", new List<string>(), "Select...".Italic(), "Package", OnPackageSelected);
    }

    void RestartVAM()
    {
        SyncLicenseCacheFile();
        SuperController.singleton.HardReset();
    }

    void ReadLicenseCacheFromFile()
    {
        var cacheJSON = FileUtils.ReadLicenseCacheJSON();
        _packageLicenseCache = JSONUtils.JsonClassToStringDictionary(cacheJSON);
    }

    void ReadSecondaryLicenseCacheFromFile()
    {
        var cacheJSON = FileUtils.ReadSecondaryLicenseCacheJSON();
        _packageSecondaryLicenseCache = new Dictionary<string, SecondaryLicenseCacheObject>();
        if(cacheJSON != null)
        {
            foreach(string key in cacheJSON.Keys)
            {
                var secondaryLicenseJSON = cacheJSON[key].AsObject;
                _packageSecondaryLicenseCache[key] = new SecondaryLicenseCacheObject
                {
                    licenseType = secondaryLicenseJSON["licenseType"].Value,
                    activeAfterDay = secondaryLicenseJSON["day"].Value,
                    activeAfterMonth = secondaryLicenseJSON["month"].Value,
                    activeAfterYear = secondaryLicenseJSON["year"].Value,
                };
            }
        }
    }

    // TODO indexing?
    void SyncLicenseCacheFile()
    {
        var cacheJSON = JSONUtils.StringDictionaryToJsonClass(_packageLicenseCache);
        FileUtils.WriteLicenseCacheJSON(cacheJSON);
    }

    // TODO indexing?
    void SyncEAEndDateCacheFile()
    {
        var cacheJSON = new JSONClass();
        if(_packageSecondaryLicenseCache != null)
        {
            foreach(var kvp in _packageSecondaryLicenseCache)
            {
                var cacheObj = kvp.Value;
                cacheJSON[kvp.Key] = new JSONClass
                {
                    ["licenseType"] = cacheObj.licenseType,
                    ["day"] = cacheObj.activeAfterDay,
                    ["month"] = cacheObj.activeAfterMonth,
                    ["year"] = cacheObj.activeAfterYear,
                };
            }
        }

        FileUtils.WriteSecondaryLicenseCacheJSON(cacheJSON);
    }

    void ReadUserPreferencesFromFile()
    {
        var prefsJSON = FileUtils.ReadPrefsJSON();
        if(prefsJSON != null)
        {
            JSONUtils.SetStorableValueFromJson(prefsJSON, addonPackagesLocationJss);
            JSONUtils.SetStorableValueFromJson(prefsJSON, alwaysEnableDefaultSessionPluginsJsb);
        }
        else
        {
            SyncPreferencesFile();
        }
    }

    void SyncPreferencesFile()
    {
        var jc = FileUtils.ReadPrefsJSON() ?? new JSONClass();
        jc[addonPackagesLocationJss.name] = addonPackagesLocationJss.val;
        jc[alwaysEnableDefaultSessionPluginsJsb.name].AsBool = alwaysEnableDefaultSessionPluginsJsb.val;
        FileUtils.WritePrefsJSON(jc);
    }

    // https://github.com/vam-community/vam-plugins-interop-specs/blob/main/keybindings.md
    public void OnBindingsListRequested(List<object> bindingsList)
    {
        InitBindings(); // Might already be setup in Init.
        bindingsList.Add(bindings.@namespace);
        bindingsList.AddRange(bindings.GetActionsList());
    }

    void InitBindings()
    {
        if(bindings)
        {
            return;
        }

        bindings = gameObject.AddComponent<Bindings>();
        bindings.Init(this, nameof(VarLicenseFilter));
    }

    List<string> _preDisabledInfoList;
    List<string> _errorsInfoList;
    List<string> _fixablePackageNames;
    List<string> _fixablePackagePaths;
    List<string> _tmpEnabledPackageNames;
    HashSet<string> _defaultSessionPluginPackages;
    bool _licenseCacheUpdated;
    bool _secondaryLicenseCacheUpdated;
    public bool requireRestart { get; private set; }
    public bool requireFixAndRestart { get; private set; }

    void InitPackages()
    {
        var tmpPackagesList = new List<VarPackage>();
        _preDisabledInfoList = new List<string>();
        _errorsInfoList = new List<string>();
        _fixablePackageNames = new List<string>();
        _fixablePackagePaths = new List<string>();
        _tmpEnabledPackageNames = FileUtils.ReadTmpEnabledPackagesFile()
            .Where(packageName => !string.IsNullOrEmpty(packageName))
            .ToList();
        _defaultSessionPluginPackages = FindPackageFilenamesFromDefaultSessionPluginsJson();
        var packageJsscOptions = new List<string>();
        var packageJsscDisplayOptions = new List<string>();
        _licenseCacheUpdated = false;
        _secondaryLicenseCacheUpdated = false;
        var dateTimeInts = new DateTimeInts(DateTime.Today);

        // TODO healthcheck on dir
        // TODO change dir

        foreach(string path in FileUtils.FindVarFilePaths(addonPackagesLocationJss.val))
        {
            string filename = Utils.BaseName(path);
            if(filename.StartsWith($"everlaster.{nameof(VarLicenseFilter)}."))
            {
                continue;
            }

            string licenseType = null;
            bool isDisabled = FileUtils.DisabledFileExists(path);
            if(_packageLicenseCache.ContainsKey(filename))
            {
                licenseType = _packageLicenseCache[filename];
            }

            bool needsMetaJson = licenseType == null;
            var secondaryLicenseCacheObject = new SecondaryLicenseCacheObject();
            if(licenseType == License.PC_EA.name)
            {
                if(_packageSecondaryLicenseCache.ContainsKey(filename))
                {
                    secondaryLicenseCacheObject = _packageSecondaryLicenseCache[filename];
                    // Debug.Log($"{filename} cached secondary license: {secondaryLicenseCacheObject}");
                }
                else
                {
                    needsMetaJson = true;
                }
            }

            bool metaJSONError = false;
            if(needsMetaJson)
            {
                var metaJSON = GetMetaJSON(path);
                if(metaJSON == null)
                {
                    _errorsInfoList.Add($"{filename}: Missing meta.json");
                    metaJSONError = true;
                }
                else
                {
                    if(string.IsNullOrEmpty(licenseType))
                    {
                        if(isDisabled)
                        {
                            _fixablePackagePaths.Add(path);
                            _fixablePackageNames.Add(filename);
                        }
                        else
                        {
                            licenseType = ReadLicenseTypeFromMetaJSON(metaJSON, filename);
                        }
                    }

                    if(licenseType == License.PC_EA.name)
                    {
                        if(!metaJSON.HasKey("secondaryLicenseType"))
                        {
                            _errorsInfoList.Add($"{filename}: Missing secondaryLicenseType field in meta.json");
                            metaJSONError = true;
                        }

                        if(!metaJSON.HasKey("EAEndDay") || !metaJSON.HasKey("EAEndMonth") || !metaJSON.HasKey("EAEndYear"))
                        {
                            _errorsInfoList.Add($"{filename}: Missing EAEndDay/EAEndMonth/EAEndYear field(s) in meta.json");
                            metaJSONError = true;
                        }
                        else
                        {
                            secondaryLicenseCacheObject.licenseType = metaJSON["secondaryLicenseType"].Value;
                            secondaryLicenseCacheObject.activeAfterDay = metaJSON["EAEndDay"].Value;
                            secondaryLicenseCacheObject.activeAfterMonth = metaJSON["EAEndMonth"].Value;
                            secondaryLicenseCacheObject.activeAfterYear = metaJSON["EAEndYear"].Value;
                            _packageSecondaryLicenseCache[filename] = secondaryLicenseCacheObject;
                            _secondaryLicenseCacheUpdated = true;
                            // Debug.Log($"{filename} meta.json secondary license: {secondaryLicenseCacheObject}");
                        }
                    }
                }
            }

            if(string.IsNullOrEmpty(licenseType) || metaJSONError)
            {
                continue;
            }

            if(!licenses.ContainsKey(licenseType))
            {
                _licenseCacheUpdated = _licenseCacheUpdated || _packageLicenseCache.Remove(filename);
                _errorsInfoList.Add($"{filename}: Unknown license type {licenseType}");
                continue;
            }

            if(licenseType == License.PC_EA.name && !licenses.ContainsKey(secondaryLicenseCacheObject.licenseType))
            {
                _secondaryLicenseCacheUpdated = _secondaryLicenseCacheUpdated || _packageSecondaryLicenseCache.Remove(filename);
                _errorsInfoList.Add($"{filename}: Unknown secondary license type {secondaryLicenseCacheObject.licenseType}");
                continue;
            }

            bool isDefaultSessionPluginPackage = _defaultSessionPluginPackages.Contains(filename);
            if(isDefaultSessionPluginPackage && alwaysEnableDefaultSessionPluginsJsb.val)
            {
                _alwaysEnabledPackages.Add(filename);
            }

            var package = new VarPackage(
                path,
                filename,
                licenses[licenseType],
                !isDisabled,
                isDefaultSessionPluginPackage,
                _alwaysEnabledPackages.Contains(filename),
                _alwaysDisabledPackages.Contains(filename)
            );

            if(licenseType == License.PC_EA.name)
            {
                package.SetSecondaryLicenseInfo(
                    new SecondaryLicenseInfo
                    {
                        license = licenses[secondaryLicenseCacheObject.licenseType],
                        activeAfterDayString = secondaryLicenseCacheObject.activeAfterDay,
                        activeAfterMonthString = secondaryLicenseCacheObject.activeAfterMonth,
                        activeAfterYearString = secondaryLicenseCacheObject.activeAfterYear,
                    },
                    dateTimeInts
                );
            }

            packageJsscOptions.Add(package.filename);
            packageJsscDisplayOptions.Add(package.displayString);

            if(isDisabled)
            {
                _preDisabledInfoList.Add(package.GetLongDisplayString());
            }

            tmpPackagesList.Add(package);
        }

        _varPackages = tmpPackagesList.OrderBy(package => package.filename).ToList();

        packageJssc.choices = packageJsscOptions;
        packageJssc.displayChoices = packageJsscDisplayOptions;

        if(_licenseCacheUpdated)
        {
            SyncLicenseCacheFile();
        }

        if(_secondaryLicenseCacheUpdated)
        {
            SyncEAEndDateCacheFile();
        }

        requireFixAndRestart = _fixablePackageNames.Count > 0;
        if(requireFixAndRestart)
        {
            if(_tmpEnabledPackageNames.Count > 0)
            {
                Loggr.Error(
                    "License info cache is still broken :(. Something may have gone wrong during creation" +
                    $" or deletion of {FileUtils.GetTmpEnabledFileFullPath()}, or during VAM restart.",
                    false
                );
            }

            var sb = new StringBuilder();
            sb.Append("\n".Size(8));
            sb.Append(_fixablePackageNames.Count);
            sb.AppendLine(
                " disabled package(s) are missing cached license info. Click the button above to temporarily" +
                $" enable these packages, allowing {nameof(VarLicenseFilter)} to cache their license info.\n"
            );
            sb.AppendLine(
                "The next time the plugin is initialized, these packages will be automatically added to the" +
                " list of packages to be disabled.\n"
            );
            sb.AppendLine(string.Join("\n", _fixablePackageNames.ToArray()));
            sb.AppendLine("");
            filterInfoJss.val = sb.ToString().Color(Colors.darkRed);
        }
        else
        {
            if(_tmpEnabledPackageNames.Count > 0)
            {
                DisableTemporarilyEnabledPackages(_tmpEnabledPackageNames);
                FileUtils.DeleteTmpEnabledPackagesFile();
                UpdateInfoPanelText(true);
            }
            else
            {
                UpdateInfoPanelText(false);
            }
        }
    }

    static HashSet<string> FindPackageFilenamesFromDefaultSessionPluginsJson()
    {
        var result = new HashSet<string>();
        var jc = FileUtils.ReadJSON("Custom\\PluginPresets\\Plugins_UserDefaults.vap");
        if(jc != null && jc.HasKey("storables"))
        {
            JSONClass plugins = null;
            foreach(var node in jc["storables"].AsArray.Childs)
            {
                var storable = node.AsObject;
                if(storable["id"].Value == "PluginManager" && storable.HasKey("plugins"))
                {
                    plugins = storable["plugins"].AsObject;
                }
            }

            if(plugins != null)
            {
                foreach(string key in plugins.Keys)
                {
                    string scriptFilename = plugins[key].Value;
                    if(scriptFilename.StartsWith($"everlaster.{nameof(VarLicenseFilter)}."))
                    {
                        continue;
                    }

                    int idx = scriptFilename.IndexOf(":/", StringComparison.Ordinal);
                    if(idx >= 0)
                    {
                        string packageName = $"{scriptFilename.Substring(0, idx)}.var";
                        result.Add(packageName);
                    }
                }
            }
        }

        return result;
    }

    JSONClass GetMetaJSON(string path)
    {
        string metaJsonPath = FileUtils.NormalizePackagePath(addonPackagesLocationJss.val, $"{path}:/meta.json");
        return FileUtils.FileExists(metaJsonPath)
            ? FileUtils.ReadJSON(metaJsonPath)
            : null;
    }

    string ReadLicenseTypeFromMetaJSON(JSONClass metaJSON, string filename)
    {
        const string licenseTypeKey = "licenseType";
        if(!metaJSON.HasKey(licenseTypeKey))
        {
            _errorsInfoList.Add($"{filename}: Missing '{licenseTypeKey}' field in meta.json");
            return null;
        }

        string licenseType = metaJSON[licenseTypeKey].Value;
        _packageLicenseCache[filename] = licenseType;
        _licenseCacheUpdated = true;
        return licenseType;
    }

    List<string> _enabledInfoList = new List<string>();
    List<string> _disabledInfoList = new List<string>();
    // static int _lineCount;
    static int _extraLineCount;

    void UpdateInfoPanelText(bool onApplyChanges)
    {
        var sb = new StringBuilder();
        sb.Append("\n".Size(8));
        // _lineCount = 0;
        _extraLineCount = 0;

        if(onApplyChanges)
        {
            if(_tmpEnabledPackageNames.Count > 0)
            {
                sb.Append(
                    "Some initially disabled packages were temporarily enabled in order to update their license\n" +
                    "info to cache. These should be visible below in the list of packages to disable.\n"
                );
            }

            if(_enabledInfoList.Count == 0 && _disabledInfoList.Count == 0)
            {
                AddLine(sb, "No changes.\n");
                AddPreDisabledInfo(sb);
            }
            else
            {
                sb.AppendLine(SummaryLine());

                if(_enabledInfoList.Count > 0)
                {
                    if(_disabledInfoList.Count > 0 || _errorsInfoList.Count > 0)
                    {
                        AddLine(sb, $"Package{(_enabledInfoList.Count > 1 ? "s" : "")} to enable:\n");
                    }

                    foreach(string line in _enabledInfoList)
                    {
                        AddLine(sb, line);
                    }

                    AddLine(sb);
                }

                if(_disabledInfoList.Count > 0)
                {
                    if(_enabledInfoList.Count > 0 || _errorsInfoList.Count > 0)
                    {
                        AddLine(sb, $"Package{(_disabledInfoList.Count > 1 ? "s" : "")} to disable:\n");
                    }

                    foreach(string line in _disabledInfoList)
                    {
                        AddLine(sb, line);
                    }

                    AddLine(sb);
                }
            }

            AddErrorsInfo(sb);
        }
        else
        {
            AddPreDisabledInfo(sb);
            AddErrorsInfo(sb);
        }

        filterInfoJss.val = _extraLineCount > 0
            ? sb + $"\n... {_extraLineCount} more rows (truncated)"
            : sb.ToString();
    }

    string SummaryLine()
    {
        var list = new List<string>();
        if(_enabledInfoList.Count > 0)
        {
            list.Add($"{_enabledInfoList.Count} package{(_enabledInfoList.Count > 1 ? "s" : "")} will be enabled");
        }

        if(_disabledInfoList.Count > 0)
        {
            string willBeDisabledText = $"{_disabledInfoList.Count} package{(_disabledInfoList.Count > 1 ? "s" : "")} will be disabled";
            int totalDisabledCount = _preDisabledInfoList.Count - _enabledInfoList.Count;
            if(totalDisabledCount > 0)
            {
                willBeDisabledText += $" (in addition to {totalDisabledCount} already disabled)";
            }

            list.Add(willBeDisabledText);
        }

        if(_errorsInfoList.Count > 0)
        {
            list.Add($"{_errorsInfoList.Count} package{(_errorsInfoList.Count > 1 ? "s" : "")} have error(s)");
        }

        return string.Join(", ", list.ToArray()).ReplaceLastOccurrence(", ", " and ") + ".\n";
    }

    static void AddLine(StringBuilder sb, string str = "")
    {
        /* Prevent ArgumentException: Mesh can not have more than 65000 vertices */
        if(sb.Length < 16000)
        {
            sb.AppendLine(str);
            // _lineCount++;
        }
        else
        {
            _extraLineCount++;
        }
    }

    void AddPreDisabledInfo(StringBuilder sb)
    {
        if(_preDisabledInfoList.Count > 0)
        {
            AddLine(sb, $"{_preDisabledInfoList.Count} package{(_preDisabledInfoList.Count > 1 ? "s" : "")} currently disabled:\n");
            foreach(string line in _preDisabledInfoList)
            {
                AddLine(sb, line);
            }

            AddLine(sb);
        }
        else
        {
            AddLine(sb, "0 packages are currently disabled.\n");
        }
    }

    void AddErrorsInfo(StringBuilder sb)
    {
        if(_errorsInfoList.Count > 0)
        {
            AddLine(sb, $"Package{(_errorsInfoList.Count > 1 ? "s" : "")} with errors:\n");
            foreach(string line in _errorsInfoList)
            {
                AddLine(sb, line);
            }

            AddLine(sb);
        }
    }

    void SaveAndContinue()
    {
        SyncPreferencesFile();
        InitPackages();
        FileUtils.WriteAlwaysEnabledCache(_alwaysEnabledPackages);
        _setupWindow.Clear();
        _mainWindow.Rebuild();
    }

    void DisableTemporarilyEnabledPackages(List<string> tmpEnabledPackagePaths)
    {
        requireRestart = false;

        try
        {
            _disabledInfoList = new List<string>();
            foreach(string path in tmpEnabledPackagePaths)
            {
                var package = _varPackages.Find(p => p.path == path);
                if(package != null)
                {
                    package.Disable();
                    if(package.changed)
                    {
                        requireRestart = true;
                        _disabledInfoList.Add(package.displayString);
                    }
                    else
                    {
                        _errorsInfoList.Add($"{package.displayString} was already disabled... OK.");
                    }
                }
                else
                {
                    _errorsInfoList.Add($"Package with path '{path}' not found!");
                }
            }

            ((MainWindow) _mainWindow).RefreshRestartButton();
        }
        catch(Exception e)
        {
            Loggr.Error($"Error disabling temporarily enabled packages: {e}");
        }
    }

    void SyncPackageStatuses()
    {
        requireRestart = false;
        _enabledInfoList = new List<string>();
        _disabledInfoList = new List<string>();

        foreach(var package in _varPackages)
        {
            package.SyncEnabled(_applyLicenseFilter);
            if(package.changed)
            {
                requireRestart = true;
                if(package.enabled)
                {
                    _enabledInfoList.Add(package.GetLongDisplayString());
                }
                else
                {
                    _disabledInfoList.Add(package.GetLongDisplayString());
                }
            }
        }

        ((MainWindow) _mainWindow).RefreshRestartButton();
        UpdateInfoPanelText(true);
    }

    void OnToggleAlwaysEnabled(bool value)
    {
        if(_selectedPackage.forceEnabled == value)
        {
            return;
        }

        _selectedPackage.forceEnabled = value;
        UpdateAlwaysEnabledListInfoText();

        if(value)
        {
            _alwaysEnabledPackages.Add(_selectedPackage.filename);
            alwaysDisableSelectedJsb.valNoCallback = false;
            if(_selectedPackage.forceDisabled)
            {
                _selectedPackage.forceDisabled = false;
                UpdateAlwaysDisabledListInfoText();
            }
        }
        else
        {
            _alwaysEnabledPackages.Remove(_selectedPackage.filename);
        }

        SyncPackageStatuses();
    }

    void OnToggleAlwaysDisabled(bool value)
    {
        if(_selectedPackage.forceDisabled == value)
        {
            return;
        }

        _selectedPackage.forceDisabled = value;
        UpdateAlwaysDisabledListInfoText();

        if(value)
        {
            _alwaysDisabledPackages.Add(_selectedPackage.filename);
            alwaysEnableSelectedJsb.valNoCallback = false;
            if(_selectedPackage.forceEnabled)
            {
                _selectedPackage.forceEnabled = false;
                UpdateAlwaysEnabledListInfoText();
            }
        }
        else
        {
            _alwaysDisabledPackages.Remove(_selectedPackage.filename);
        }

        SyncPackageStatuses();
    }

    void OnToggleAlwaysEnableDefaultSessionPlugins(bool value)
    {
        if(!_lateInitDone)
        {
            return;
        }

        SyncPreferencesFile();

        foreach(var package in _varPackages)
        {
            if(package.isDefaultSessionPluginPackage)
            {
                package.forceEnabled = value;
                if(value)
                {
                    package.forceDisabled = false;
                    _alwaysEnabledPackages.Add(package.filename);
                    _alwaysDisabledPackages.Remove(package.filename);
                }
                else
                {
                    _alwaysEnabledPackages.Remove(package.filename);
                }

                if(package == _selectedPackage)
                {
                    alwaysEnableSelectedJsb.valNoCallback = package.forceEnabled;
                    alwaysDisableSelectedJsb.valNoCallback = package.forceDisabled;
                }
            }
        }

        UpdateAlwaysEnabledListInfoText();
        UpdateAlwaysDisabledListInfoText();
        SyncPackageStatuses();
    }

    void OnPackageSelected(string value)
    {
        _selectedPackage = FindPackage(packageJssc.val);
        if(_selectedPackage != null)
        {
            alwaysEnableSelectedJsb.valNoCallback = _selectedPackage.forceEnabled;
            alwaysDisableSelectedJsb.valNoCallback = _selectedPackage.forceDisabled;
        }
    }

    public void UpdateAlwaysEnabledListInfoText()
    {
        var sb = new StringBuilder();
        sb.Append("\n".Size(8));

        var list = new List<string>();
        foreach(var package in _varPackages)
        {
            if(package.forceEnabled)
            {
                list.Add(package.isDefaultSessionPluginPackage ? package.displayString.Color(Colors.sessionPluginColor) : package.displayString);
            }
        }

        if(list.Count > 0)
        {
            sb.AppendLine(string.Join("\n", list.ToArray()));
            sb.AppendLine("");
        }

        alwaysEnabledListInfoJss.val = sb.ToString();
    }

    public void UpdateAlwaysDisabledListInfoText()
    {
        var sb = new StringBuilder();
        sb.Append("\n".Size(8));

        var list = new List<string>();
        foreach(var package in _varPackages)
        {
            if(package.forceDisabled)
            {
                list.Add(package.displayString);
            }
        }

        if(list.Count > 0)
        {
            sb.AppendLine(string.Join("\n", list.ToArray()));
            sb.AppendLine("");
        }

        alwaysDisabledListInfoJss.val = sb.ToString();
    }

    VarPackage FindPackage(string filename)
    {
        if(string.IsNullOrEmpty(filename) || filename == packageJssc.defaultVal)
        {
            return null;
        }

        try
        {
            return _varPackages.Find(package => package.filename == filename);
        }
        catch(Exception e)
        {
            Loggr.Error($"Error finding package {filename}: {e}");
            return null;
        }
    }

#endregion

    public override void RestoreFromJSON(
        JSONClass jsonClass,
        bool restorePhysical = true,
        bool restoreAppearance = true,
        JSONArray presetAtoms = null,
        bool setMissingToDefault = true
    )
    {
        /* Disable early to allow correct enabled value to be used during Init */
        if(jsonClass.HasKey("enabled") && !jsonClass["enabled"].AsBool)
        {
            enabled = false;
        }

        /* Prevent overriding versionJss.val from JSON. Version stored in JSON just for information,
         * but could be intercepted here and used to save a "loadedFromVersion" value.
         */
        if(jsonClass.HasKey(Constant.VERSION))
        {
            jsonClass[Constant.VERSION] = VERSION;
        }

        StartCoroutine(
            RestoreFromJSONCo(
                jsonClass,
                restorePhysical,
                restoreAppearance,
                presetAtoms,
                setMissingToDefault
            )
        );
    }

    IEnumerator RestoreFromJSONCo(
        JSONClass jsonClass,
        bool restorePhysical,
        bool restoreAppearance,
        JSONArray presetAtoms,
        bool setMissingToDefault
    )
    {
        while(initialized == null)
        {
            yield return null;
        }

        if(initialized == false)
        {
            yield break;
        }

        base.RestoreFromJSON(jsonClass, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);
    }

    public void AddTextFieldToJss(UIDynamicTextField textField, JSONStorableString jss)
    {
        jss.dynamicText = textField;
        textFieldToJSONStorableString.Add(textField, jss);
    }

    public void AddToggleToJsb(UIDynamicToggle toggle, JSONStorableBool jsb)
    {
        jsb.toggle = toggle.toggle;
        toggleToJSONStorableBool.Add(toggle, jsb);
    }

    public void AddSliderToJsf(UIDynamicSlider slider, JSONStorableFloat jsf)
    {
        jsf.slider = slider.slider;
        sliderToJSONStorableFloat.Add(slider, jsf);
    }

    public void AddPopupToJssc(UIDynamicPopup uiDynamicPopup, JSONStorableStringChooser jssc)
    {
        jssc.popup = uiDynamicPopup.popup;
        popupToJSONStorableStringChooser.Add(uiDynamicPopup, jssc);
    }

    void OnEnable()
    {
        if(initialized != true)
        {
            return;
        }

        try
        {
        }
        catch(Exception e)
        {
            Loggr.Error($"{nameof(OnEnable)} error: {e}");
        }
    }

    void OnDisable()
    {
        if(initialized != true)
        {
            return;
        }

        try
        {
        }
        catch(Exception e)
        {
            Loggr.Error($"{nameof(OnDisable)} error: {e}");
        }
    }

    new void OnDestroy()
    {
        try
        {
            base.OnDestroy();
            Destroy(bindings);
            /* Nullify static reference fields to let GC collect unreachable instances */
            script = null;
            SuperController.singleton.BroadcastMessage("OnActionsProviderDestroyed", this, SendMessageOptions.DontRequireReceiver);
        }
        catch(Exception e)
        {
            if(initialized == true)
            {
                SuperController.LogError($"OnDestroy: {e}");
            }
            else
            {
                Debug.LogError($"{nameof(OnDestroy)}: {e}");
            }
        }
    }
}
