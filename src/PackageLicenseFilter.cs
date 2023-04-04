#define ENV_DEVELOPMENT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using UnityEngine;

sealed class PackageLicenseFilter : ScriptBase
{
    /* Public static access point to plugin instance. */
    public const string VERSION = "0.0.0";
    public static bool envIsDevelopment { get; private set; }
    public static PackageLicenseFilter script { get; private set; }

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

            if(containingAtom.FindStorablesByRegexMatch(Utils.NewRegex($@"^plugin#\d+_{nameof(PackageLicenseFilter)}")).Count > 0)
            {
                FailInitWithMessage($"An instance of {nameof(PackageLicenseFilter)} is already added.");
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
    readonly List<VarPackage> _varPackages = new List<VarPackage>();
    public readonly Dictionary<string, License> licenseTypes = new Dictionary<string, License>
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
    public JSONStorableAction resetStatusesAction { get; private set; }
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
                return;
            }

            SetupStorables();
            ReadLicenseCacheFromFile();
            ReadUserPreferencesFromFile();
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
        alwaysEnableSelectedJsb = new JSONStorableBool("Always enable selected", false, OnToggleAlwaysEnabled);
        alwaysDisableSelectedJsb = new JSONStorableBool("Always disable selected", false, OnToggleAlwaysDisabled);
        alwaysEnabledListInfoJss = new JSONStorableString("Always enabled list info", "");
        alwaysDisabledListInfoJss = new JSONStorableString("Always disabled list info", "");
        applyFilterAction = new JSONStorableAction("Apply license filter to packages", () =>
        {
            _applyLicenseFilter = true;
            SyncPackageStatuses();
        });
        resetStatusesAction = new JSONStorableAction("Reset license filter", () =>
        {
            _applyLicenseFilter = false;
            foreach(var kvp in licenseTypes)
            {
                kvp.Value.enabledJsb.val = kvp.Value.enabledJsb.defaultVal;
            }

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
        restartVamAction = new JSONStorableAction("Restart VAM to apply changes", () =>
        {
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
        alwaysEnableDefaultSessionPluginsJsb = new JSONStorableBool("Always enable default session plugin packages", true);
        packageJssc = new JSONStorableStringChooser("Package", new List<string>(), "Select...".Italic(), "Package", OnPackageSelected);
    }

    void RestartVAM()
    {
        SyncLicenseCacheFile();
        SuperController.singleton.HardReset();
    }

    void ReadLicenseCacheFromFile()
    {
        var cacheJson = FileUtils.ReadLicenseCacheJSON();
        _packageLicenseCache = JSONUtils.JsonClassToStringDictionary(cacheJson);
    }

    // TODO indexing?
    void SyncLicenseCacheFile()
    {
        var cacheJson = JSONUtils.StringDictionaryToJsonClass(_packageLicenseCache);
        FileUtils.WriteLicenseCacheJSON(cacheJson);
    }

    void ReadUserPreferencesFromFile()
    {
        var prefsJson = FileUtils.ReadPrefsJSON();
        if(prefsJson != null && prefsJson.HasKey(addonPackagesLocationJss.name))
        {
            // TODO validate
            JSONUtils.SetStorableValueFromJson(prefsJson, addonPackagesLocationJss);
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
        bindings.Init(this, nameof(PackageLicenseFilter));
    }

    List<string> _preDisabledInfoList;
    List<string> _errorsInfoList;
    List<string> _fixablePackageNames;
    List<string> _fixablePackagePaths;
    List<string> _tmpEnabledPackageNames;
    List<string> _disabledInfoList;
    List<string> _enabledInfoList;
    bool _cacheUpdated;
    public bool requireRestart { get; private set; }
    public bool requireFixAndRestart { get; private set; }

    void InitPackages()
    {
        _preDisabledInfoList = new List<string>();
        _errorsInfoList = new List<string>();
        _fixablePackageNames = new List<string>();
        _fixablePackagePaths = new List<string>();
        _tmpEnabledPackageNames = FileUtils.ReadTmpEnabledPackagesFile()
            .Split('\n')
            .Where(packageName => !string.IsNullOrEmpty(packageName))
            .ToList();
        var packageJsscOptions = new List<string>();
        var packageJsscDisplayOptions = new List<string>();
        _cacheUpdated = false;
        string thisPackageName = this.GetPackageName();

        // TODO healthcheck on dir
        // TODO change dir

        foreach(string path in FileUtils.FindVarFilePaths(addonPackagesLocationJss.val))
        {
            string fileName = Utils.BaseName(path);
            if(!string.IsNullOrEmpty(thisPackageName) && fileName.StartsWith(thisPackageName))
            {
                continue;
            }

            bool isDisabled = FileUtils.DisabledFileExists(path);
            string licenseType = isDisabled ? ReadDisabledPackageLicenseType(path, fileName) : ReadEnabledPackageLicenseType(path, fileName);
            if(string.IsNullOrEmpty(licenseType))
            {
                continue;
            }

            if(!licenseTypes.ContainsKey(licenseType))
            {
                _cacheUpdated = _cacheUpdated || _packageLicenseCache.Remove(fileName);
                _errorsInfoList.Add($"{fileName}: Unknown license type {licenseType}");
                continue;
            }

            var package = new VarPackage(path, fileName, licenseTypes[licenseType], !isDisabled);
            packageJsscOptions.Add(package.fileName);
            packageJsscDisplayOptions.Add(package.displayString);

            if(isDisabled)
            {
                _preDisabledInfoList.Add(package.displayString);
            }

            _varPackages.Add(package);
        }

        packageJssc.choices = packageJsscOptions;
        packageJssc.displayChoices = packageJsscDisplayOptions;

        if(_cacheUpdated)
        {
            SyncLicenseCacheFile();
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
                $" enable these packages, allowing {nameof(PackageLicenseFilter)} to cache their license info.\n"
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

    string ReadDisabledPackageLicenseType(string path, string fileName)
    {
        if(!_packageLicenseCache.ContainsKey(fileName))
        {
            _fixablePackagePaths.Add(path);
            _fixablePackageNames.Add(fileName);
            return null;
        }

        return _packageLicenseCache[fileName];
    }

    string ReadEnabledPackageLicenseType(string path, string fileName)
    {
        if(_packageLicenseCache.ContainsKey(fileName))
        {
            return _packageLicenseCache[fileName];
        }

        string metaJsonPath = FileUtils.NormalizePackagePath(addonPackagesLocationJss.val, $"{path}:/meta.json");
        if(!FileUtils.FileExists(metaJsonPath))
        {
            _errorsInfoList.Add($"{fileName}: Missing meta.json");
            return null;
        }

        const string licenseTypeKey = "licenseType";
        var metaJson = FileUtils.ReadJSON(metaJsonPath) ?? new JSONClass();
        if(!metaJson.HasKey(licenseTypeKey))
        {
            _errorsInfoList.Add($"{fileName}: Missing '{licenseTypeKey}' field in meta.json");
            return null;
        }

        string licenseType = metaJson[licenseTypeKey].Value;
        _packageLicenseCache[fileName] = licenseType;
        _cacheUpdated = true;
        return licenseType;
    }

    void UpdateInfoPanelText(bool onApplyChanges)
    {
        var sb = new StringBuilder();
        sb.Append("\n".Size(8));

        if(onApplyChanges)
        {
            if(_tmpEnabledPackageNames.Count > 0)
            {
                sb.AppendLine(
                    "Some initially disabled packages were temporarily enabled in order to update their license info to cache." +
                    " These should be visible below in the list of packages to disable.\n"
                );
            }

            bool willEnable = _enabledInfoList != null && _enabledInfoList.Count > 0;
            if(willEnable)
            {
                sb.AppendLine($"{_enabledInfoList.Count} package(s) will be enabled:\n");
                sb.AppendLine(string.Join("\n", _enabledInfoList.ToArray()));
                sb.AppendLine("");
            }

            bool willDisable = _disabledInfoList != null && _disabledInfoList.Count > 0;
            if(willDisable)
            {
                sb.Append($"{_disabledInfoList.Count} package(s) will be disabled");
                if(_preDisabledInfoList.Count > 0)
                {
                    sb.Append($" (in addition to {_preDisabledInfoList.Count} already disabled)");
                }

                sb.AppendLine(":\n");
                sb.AppendLine(string.Join("\n", _disabledInfoList.ToArray()));
                sb.AppendLine("");
            }

            AddErrorsInfo(sb);

            if(!willEnable && !willDisable)
            {
                sb.AppendLine("No changes.\n");
                AddPreDisabledInfo(sb);
            }
        }
        else
        {
            AddErrorsInfo(sb);
            AddPreDisabledInfo(sb);
        }

        filterInfoJss.val = sb.ToString();
    }

    void AddPreDisabledInfo(StringBuilder sb)
    {
        if(_preDisabledInfoList.Count > 0)
        {
            sb.AppendLine($"{_preDisabledInfoList.Count} package(s) are currently disabled:\n");
            sb.AppendLine(string.Join("\n", _preDisabledInfoList.ToArray()));
            sb.AppendLine("");
        }
        else
        {
            sb.AppendLine("0 packages are currently disabled.\n");
        }
    }

    void AddErrorsInfo(StringBuilder sb)
    {
        if(_errorsInfoList.Count > 0)
        {
            sb.AppendLine($"{_errorsInfoList.Count} package(s) have errors:\n");
            sb.AppendLine(string.Join("\n", _errorsInfoList.ToArray()));
            sb.AppendLine("");
        }
    }

    void SaveAndContinue()
    {
        SyncPreferencesFile();
        InitPackages();
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
                    _enabledInfoList.Add(package.displayString);
                }
                else
                {
                    _disabledInfoList.Add(package.displayString);
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
            alwaysDisableSelectedJsb.valNoCallback = false;
            if(_selectedPackage.forceDisabled)
            {
                _selectedPackage.forceDisabled = false;
                UpdateAlwaysDisabledListInfoText();
            }
        }

        SyncPackageStatuses();
    }

    void OnToggleAlwaysDisabled(bool value)
    {
        if(_selectedPackage.forceDisabled == value)
        {
            return;
        }

        Debug.Log($"{_selectedPackage.fileName}: OnToggleAlwaysDisabled: {value}");

        _selectedPackage.forceDisabled = value;
        UpdateAlwaysDisabledListInfoText();

        if(value)
        {
            alwaysEnableSelectedJsb.valNoCallback = false;
            if(_selectedPackage.forceEnabled)
            {
                _selectedPackage.forceEnabled = false;
                UpdateAlwaysEnabledListInfoText();
            }
        }

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
                list.Add(package.fileName);
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
                list.Add(package.fileName);
            }
        }

        if(list.Count > 0)
        {
            sb.AppendLine(string.Join("\n", list.ToArray()));
            sb.AppendLine("");
        }

        alwaysDisabledListInfoJss.val = sb.ToString();
    }

    VarPackage FindPackage(string fileName)
    {
        if(string.IsNullOrEmpty(fileName) || fileName == packageJssc.defaultVal)
        {
            return null;
        }

        try
        {
            return _varPackages.Find(package => package.fileName == fileName);
        }
        catch(Exception e)
        {
            Loggr.Error($"Error finding package {fileName}: {e}");
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
            // TODO restore filtered statuses and refresh packages
            // ApplyFilter();
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
