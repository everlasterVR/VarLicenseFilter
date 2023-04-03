#define ENV_DEVELOPMENT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MVR.FileManagementSecure;
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
    public JSONStorableAction applyFilterAction { get; private set; }
    public JSONStorableAction restartVamAction { get; private set; }
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
            FindAddonPackagesDirPaths();
            SetupStorables();
            ReadUserPreferencesFromFile();
            InitBindings(); // Might already be setup in OnBindingsListRequested.
            SuperController.singleton.BroadcastMessage("OnActionsProviderAvailable", this, SendMessageOptions.DontRequireReceiver);

            _mainWindow = new MainWindow();
            _setupWindow = new SetupWindow();
            if(string.IsNullOrEmpty(addonPackagesLocationJss.val))
            {
                _setupWindow.Rebuild();
            }
            else
            {
                _mainWindow.Rebuild();
                InitPackages();
            }

            initialized = true;
        }
        catch(Exception e)
        {
            FailInitWithError($"Init error: {e}");
        }
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
        applyFilterAction = new JSONStorableAction("Apply filter", ApplyFilter);
        restartVamAction = new JSONStorableAction("Restart VAM", SuperController.singleton.HardReset);
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
        bindingsList.Add(bindings.Namespace());
        bindingsList.AddRange(bindings.Actions());
    }

    void InitBindings()
    {
        if(bindings)
        {
            return;
        }

        bindings = gameObject.AddComponent<Bindings>();
        bindings.Init();
    }

    void InitPackages()
    {
        var errorPackages = new StringBuilder();
        int enabledPackagesCount = 0;
        int disabledPackagesCount = 0;
        string thisPackageName = this.GetPackageName();

        foreach(string path in FileUtils.FindVarFilePaths())
        {
            string fileName = Utils.BaseName(path);
            if(!string.IsNullOrEmpty(thisPackageName) && fileName.StartsWith(thisPackageName))
            {
                continue;
            }

            string metaJsonPath = path + ":/meta.json";
            if(!FileManagerSecure.FileExists(metaJsonPath))
            {
                errorPackages.AppendLine($"{fileName}: Missing meta.json");
                continue;
            }

            var metaJson = FileUtils.ReadJSON(metaJsonPath) ?? new JSONClass();
            if(!metaJson.HasKey("licenseType"))
            {
                errorPackages.AppendLine($"{fileName}: Missing 'licenseType' field in meta.json");
                continue;
            }

            string licenseString = metaJson["licenseType"];
            if(!licenseTypes.ContainsKey(licenseString))
            {
                errorPackages.AppendLine($"{fileName}: Unknown license {licenseString}");
                continue;
            }

            var varPackage = new VarPackage(path, fileName, licenseTypes[licenseString], addonPackagesLocationJss.val);
            _varPackages.Add(varPackage);
            if(varPackage.enabled)
            {
                enabledPackagesCount++;
            }
            else
            {
                disabledPackagesCount++;
            }
        }

        var infoText = new StringBuilder();
        infoText.Append("\n".Size(8));
        infoText.AppendLine($"Enabled packages count: {enabledPackagesCount}".Bold());
        infoText.AppendLine($"Disabled packages count: {disabledPackagesCount}".Bold());

        if(errorPackages.Length > 0)
        {
            infoText.AppendLine("Packages with license errors:\n".Bold());
            infoText.AppendLine(errorPackages.ToString());
        }

        filterInfoJss.val = infoText.ToString();
    }

    void SaveAndContinue()
    {
        SyncPreferencesFile();
        InitPackages();
        _setupWindow.Clear();
        _mainWindow.Rebuild();
    }

    void ApplyFilter()
    {
        int enabledPackagesCount = 0;
        int disabledPackagesCount = 0;
        var changedPackagesList = new StringBuilder();

        foreach(var package in _varPackages)
        {
            bool statusChanged = package.SyncStatus();
            if(statusChanged)
            {
                Color color;
                if(package.enabled)
                {
                    color = Color.green;
                    enabledPackagesCount++;
                }
                else
                {
                    color = Color.red;
                    disabledPackagesCount++;
                }

                changedPackagesList.AppendLine($"{package.license.name.Color(color)}  {package.name}");
            }
        }

        var infoText = new StringBuilder();
        infoText.Append("\n".Size(8));

        if(enabledPackagesCount > 0 && disabledPackagesCount > 0)
        {
            infoText.AppendLine(
                $"{enabledPackagesCount} packages will be enabled and " +
                $"{disabledPackagesCount} packages will be disabled." +
                " VAM restart needed!\n"
            );
            infoText.AppendLine(changedPackagesList.ToString());
        }
        else if(enabledPackagesCount > 0)
        {
            infoText.AppendLine($"{enabledPackagesCount} packages will be enabled. VAM restart needed!\n");
            infoText.AppendLine(changedPackagesList.ToString());
        }
        else if(disabledPackagesCount > 0)
        {
            infoText.AppendLine($"{disabledPackagesCount} packages will be disabled. VAM restart needed!\n");
            infoText.AppendLine(changedPackagesList.ToString());
        }

        filterInfoJss.val = infoText.ToString();
        // ((MainWindow) _mainWindow).SyncApplyFilterButtonText(_packagesStatusChanged);
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

    void OnEnable()
    {
        if(initialized != true)
        {
            return;
        }

        try
        {
            // TODO restore filtered statuses and refresh packages
            ApplyFilter();
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
