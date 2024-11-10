using everlaster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using UnityEngine;

sealed class VarLicenseFilter : Script
{
    public override bool ShouldIgnore() => false;
    public override string className => nameof(VarLicenseFilter);
    protected override bool preventDisable => true;

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
    public StorableString addonPackagesLocationString { get; private set; }
    public StorableString filterInfoString { get; private set; }
    public StorableBool alwaysEnableSelectedBool { get; private set; }
    public StorableBool alwaysDisableSelectedBool { get; private set; }
    public StorableString alwaysEnabledListInfoString { get; private set; }
    public StorableString alwaysDisabledListInfoString { get; private set; }
    public StorableAction applyFilterAction { get; private set; }
    public StorableAction undoRunFiltersAction { get; private set; }
    public StorableAction fixAndRestartAction { get; private set; }
    public StorableAction restartVamAction { get; private set; }
    public StorableBool alwaysEnableDefaultSessionPluginsBool { get; private set; }
    public StorableStringChooser packageJssc { get; private set; }

    VarPackage _selectedPackage;
    bool _applyLicenseFilter;
    public UIHandler uiHandler { get; private set; }

    protected override void OnInit()
    {
        if(!IsValidAtomType(AtomType.SESSION_PLUGIN_MANAGER))
        {
            return;
        }

        initialized = true;
    }

    // Use first time UI enable to initialize
    protected override void CreateUI()
    {
        SetupStorables();
        ReadUserPreferencesFromFile();
        ReadLicenseCacheFromFile();
        ReadSecondaryLicenseCacheFromFile();
        _alwaysEnabledPackages = new HashSet<string>(FileUtils.ReadAlwaysEnabledCache());
        _alwaysDisabledPackages = new HashSet<string>(FileUtils.ReadAlwaysDisabledCache());
        uiHandler = new UIHandler(this);
        if(string.IsNullOrEmpty(addonPackagesLocationString.val) || !FileUtils.DirectoryExists(addonPackagesLocationString.val))
        {
            FindAddonPackagesDirPaths();
            uiHandler.GoToSetupWindow();
        }
        else
        {
            InitPackages();
            uiHandler.GoToMainWindow();
        }
    }

    protected override void OnUIEnabled()
    {
        if(!string.IsNullOrEmpty(addonPackagesLocationString.val) && !requireFixAndRestart)
        {
            bool syncNeeded = RefreshSessionPluginPackages();
            if(syncNeeded)
            {
                UpdateAlwaysEnabledListInfoText();
                UpdateAlwaysDisabledListInfoText();
                SyncPackageStatuses();
            }
        }
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

            if(alwaysEnableDefaultSessionPluginsBool.val)
            {
                syncNeeded = true;
                _alwaysEnabledPackages.Add(package.filename);
                _alwaysDisabledPackages.Remove(package.filename);
                package.forceEnabled = true;
                package.forceDisabled = false;

                if(package == _selectedPackage)
                {
                    alwaysEnableSelectedBool.valNoCallback = package.forceEnabled;
                    alwaysDisableSelectedBool.valNoCallback = package.forceDisabled;
                }
            }
        }

        return syncNeeded;
    }

    void FindAddonPackagesDirPaths()
    {
        addonPackagesDirPaths = new List<string>();
        addonPackagesDirPaths.AddRange(FileUtils.FindAddonPackagesInPluginDataDirPaths("Custom"));
        addonPackagesDirPaths.AddRange(FileUtils.FindAddonPackagesInPluginDataDirPaths("Saves"));
    }

    void SetupStorables()
    {
        addonPackagesLocationString = new StorableString("AddonPackages location", "");
        filterInfoString = new StorableString("Filter info", "");

        alwaysEnableSelectedBool = new StorableBool("Always enable selected package", false);
        alwaysEnableSelectedBool.SetCallback(OnToggleAlwaysEnabled);

        alwaysDisableSelectedBool = new StorableBool("Always disable selected package", false);
        alwaysDisableSelectedBool.SetCallback(OnToggleAlwaysDisabled);

        alwaysEnabledListInfoString = new StorableString("Always enabled list info", "");
        alwaysDisabledListInfoString = new StorableString("Always disabled list info", "");

        applyFilterAction = new StorableAction("Run filters and preview result", () =>
        {
            _applyLicenseFilter = true;
            SyncPackageStatuses();
        });

        undoRunFiltersAction = new StorableAction("Undo run filters", () =>
        {
            _applyLicenseFilter = false;
            SyncPackageStatuses();
        });

        fixAndRestartAction = new StorableAction("Fix cache and restart VAM", () =>
        {
            foreach(string path in _fixablePackagePaths)
            {
                FileUtils.DeleteDisabledFile(path);
            }

            FileUtils.WriteTmpEnabledPackagesFile(string.Join("\n", _fixablePackagePaths.ToArray()));
            RestartVAM();
        });

        restartVamAction = new StorableAction("Save changes and restart VAM", () =>
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

        alwaysEnableDefaultSessionPluginsBool = new StorableBool(
            "Always enable default session plugin packages",
            "Always enable default session\nplugin packages",
            true
        );
        alwaysEnableDefaultSessionPluginsBool.SetCallback(OnToggleAlwaysEnableDefaultSessionPlugins);

        packageJssc = new StorableStringChooser("Package", new List<string>(), Strings.SELECT);
        packageJssc.SetCallback(OnPackageSelected);
    }

    void RestartVAM()
    {
        SyncLicenseCacheFile();
        SuperController.singleton.HardReset();
    }

    void ReadLicenseCacheFromFile()
    {
        var dict = new Dictionary<string, string>();
        var cacheJSON = FileUtils.ReadLicenseCacheJSON();
        if(cacheJSON != null)
        {
            foreach(string key in cacheJSON.Keys)
            {
                dict[key] = cacheJSON[key];
            }
        }

        _packageLicenseCache = dict;
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
        var cacheJSON = new JSONClass();
        if(_packageLicenseCache != null)
        {
            foreach(var kvp in _packageLicenseCache)
            {
                cacheJSON[kvp.Key] = kvp.Value;
            }
        }

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
            addonPackagesLocationString.SetValueFromJSON(prefsJSON);
            alwaysEnableDefaultSessionPluginsBool.SetValueFromJSON(prefsJSON);
        }
        else
        {
            SyncPreferencesFile();
        }
    }

    void SyncPreferencesFile()
    {
        var jc = FileUtils.ReadPrefsJSON() ?? new JSONClass();
        jc[addonPackagesLocationString.name] = addonPackagesLocationString.val;
        jc[alwaysEnableDefaultSessionPluginsBool.name].AsBool = alwaysEnableDefaultSessionPluginsBool.val;
        FileUtils.WritePrefsJSON(jc);
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

        foreach(string path in FileUtils.FindVarFilePaths(addonPackagesLocationString.val))
        {
            string filename = path.BaseName();
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
                    if(string.IsNullOrEmpty(licenseType) && isDisabled)
                    {
                        _fixablePackagePaths.Add(path);
                        _fixablePackageNames.Add(filename);
                    }
                    else
                    {
                        _errorsInfoList.Add($"{filename}: Missing meta.json");
                        metaJSONError = true;
                    }
                }
                else
                {
                    if(string.IsNullOrEmpty(licenseType))
                    {
                        licenseType = ReadLicenseTypeFromMetaJSON(metaJSON, filename);
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
            if(isDefaultSessionPluginPackage && alwaysEnableDefaultSessionPluginsBool.val)
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
                logBuilder.Error(
                    "License info cache is still broken :(. Something may have gone wrong during creation" +
                    $" or deletion of {FileUtils.GetTmpEnabledFileFullPath()}, or during VAM restart.",
                    false
                );
            }

            var sb = new StringBuilder();
            sb.Append(_fixablePackageNames.Count);
            sb.AppendLine(
                " disabled package(s) are missing cached license info. Click the button above to temporarily\n" +
                $"enable these packages, allowing {nameof(VarLicenseFilter)} to cache their license info.\n"
            );
            sb.AppendLine(
                "The next time the plugin is initialized, these packages will be automatically added to the list\n" +
                "of packages to be disabled. (Another restart will be required to actually disable them.)\n"
            );
            sb.AppendLine(string.Join("\n", _fixablePackageNames.ToArray()));
            sb.AppendLine("");
            filterInfoString.val = sb.ToString().Color(Colors.darkErrorColor);
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
        var jc = FileUtils.ReadJSON(@"Custom\PluginPresets\Plugins_UserDefaults.vap");
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
        string metaJsonPath = FileUtils.NormalizePackagePath(addonPackagesLocationString.val, $"{path}:/meta.json");
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
        // _lineCount = 0;
        _extraLineCount = 0;

        if(onApplyChanges)
        {
            if(_tmpEnabledPackageNames.Count > 0)
            {
                sb.AppendLine(
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

        filterInfoString.val = _extraLineCount > 0
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
            list.Add($"{_errorsInfoList.Count} package(s) have an errors.");
        }

        return string.Join(", ", list.ToArray()).ReplaceLast(", ", " and ") + ".\n";
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

    public void SaveAndContinue()
    {
        SyncPreferencesFile();
        InitPackages();
        FileUtils.WriteAlwaysEnabledCache(_alwaysEnabledPackages);
        uiHandler.GoToMainWindow();
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

            uiHandler?.GetActiveWindow()?.Refresh();
        }
        catch(Exception e)
        {
            logBuilder.Exception(e);
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

        uiHandler?.GetActiveWindow()?.Refresh();
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
            alwaysDisableSelectedBool.valNoCallback = false;
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
            alwaysEnableSelectedBool.valNoCallback = false;
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
        if(!uiCreated)
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
                    alwaysEnableSelectedBool.valNoCallback = package.forceEnabled;
                    alwaysDisableSelectedBool.valNoCallback = package.forceDisabled;
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
            alwaysEnableSelectedBool.valNoCallback = _selectedPackage.forceEnabled;
            alwaysDisableSelectedBool.valNoCallback = _selectedPackage.forceDisabled;
        }
    }

    public void UpdateAlwaysEnabledListInfoText()
    {
        var sb = new StringBuilder();
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

        alwaysEnabledListInfoString.val = sb.ToString();
    }

    public void UpdateAlwaysDisabledListInfoText()
    {
        var sb = new StringBuilder();
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

        alwaysDisabledListInfoString.val = sb.ToString();
    }

    VarPackage FindPackage(string filename)
    {
        try
        {
            if(string.IsNullOrEmpty(filename) || filename == packageJssc.defaultVal)
            {
                return null;
            }

            return _varPackages.Find(package => package.filename == filename);
        }
        catch(Exception e)
        {
            logBuilder.Exception(e);
            return null;
        }
    }
}
