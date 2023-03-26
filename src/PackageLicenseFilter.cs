#define ENV_DEVELOPMENT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public override bool ShouldIgnore() => false;

#region *** Init ***

    public static bool isInitialized => script.initialized == true;

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

            if(containingAtom.type != "SessionPluginManager")
            {
                string incorrectTargetStr = containingAtom.type == "CoreControl" ? "Scene Plugins" : $"atom {containingAtom.type}";
                FailInitWithMessage($"Add to Session Plugins, not to {incorrectTargetStr}.");
                return;
            }

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

    readonly Dictionary<string, bool> _licenseTypesEnabled = new Dictionary<string, bool>()
    {
        { "PC", true },
        { "PC EA", true },
        { "Questionable", true },
        { "FC", true },
        { "CC BY", true },
        { "CC BY-SA", true },
        { "CC BY-ND", true },
        { "CC BY-NC", true },
        { "CC BY-NC-SA", true },
        { "CC BY-NC-ND", true },
    };
    public List<JSONStorableBool> licenseTypeEnabledJsons { get; private set; }

    public JSONStorableString restartVamInfoJss { get; private set; }
    public JSONStorableString filterInfoJss { get; private set; }
    public JSONStorableAction applyFilterAction { get; private set; }

    IWindow _mainWindow;

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
            InitStorables();
            InitPackages();

            _mainWindow = new MainWindow();
            _mainWindow.Rebuild();

            initialized = true;
        }
        catch(Exception e)
        {
            FailInitWithError($"Init error: {e}");
        }
    }

    void InitStorables()
    {
        licenseTypeEnabledJsons = _licenseTypesEnabled.Keys.ToList()
            .Select(
                licenseType =>
                {
                    var jsb = this.NewJSONStorableBool(licenseType, true);
                    jsb.setCallbackFunction = value => _licenseTypesEnabled[licenseType] = value;
                    return jsb;
                }
            )
            .ToList();
        restartVamInfoJss = new JSONStorableString("Restart VAM Info", "");
        filterInfoJss = new JSONStorableString("Filter Info", "");
        applyFilterAction = new JSONStorableAction("Apply Filter", ApplyFilter);
    }

    void InitPackages()
    {
        var errorPackages = new StringBuilder();
        int enabledPackagesCount = 0;
        int disabledPackagesCount = 0;

        foreach(string path in FileUtils.FindVarFilePaths())
        {
            string fileName = Utils.BaseName(path);
            string metaJsonPath = path + ":\\meta.json";
            if(!FileManagerSecure.FileExists(metaJsonPath))
            {
                errorPackages.AppendLine($"{fileName}: Missing meta.json");
                continue;
            }

            var metaJson = FileUtils.ReadJSON(metaJsonPath);
            if(!metaJson.HasKey("licenseType"))
            {
                errorPackages.AppendLine($"{fileName}: Missing 'licenseType' field in meta.json");
                continue;
            }

            string license = metaJson["licenseType"];
            if(!_licenseTypesEnabled.ContainsKey(license))
            {
                errorPackages.AppendLine($"{fileName}: Unknown license {license}");
                continue;
            }

            bool packageEnabled = _licenseTypesEnabled[license];
            _varPackages.Add(new VarPackage(path, fileName, license, packageEnabled));
            if(packageEnabled)
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

    bool _packagesDirty;

    void ApplyFilter()
    {
        int enabledPackagesCount = 0;
        int disabledPackagesCount = 0;
        var enabledPackagesList = new StringBuilder();
        var disabledPackagesList = new StringBuilder();

        bool anyStatusChanged = false;
        foreach(var package in _varPackages)
        {
            string license = package.license;
            bool packageEnabled = _licenseTypesEnabled[license];
            package.SetStatus(packageEnabled);
            if(package.dirty)
            {
                if(packageEnabled)
                {
                    enabledPackagesCount++;
                    enabledPackagesList.AppendLine($"{package.name}: {license}");
                }
                else
                {
                    disabledPackagesCount++;
                    disabledPackagesList.AppendLine($"{package.name}: {license}");
                }
            }

            anyStatusChanged = anyStatusChanged || package.dirty;
        }

        var infoText = new StringBuilder();
        infoText.Append("\n".Size(8));

        if(enabledPackagesCount > 0)
        {
            infoText.AppendLine($"{enabledPackagesCount} packages will be enabled:\n".Bold());
            infoText.AppendLine(enabledPackagesList.ToString());
        }

        if(disabledPackagesCount > 0)
        {
            infoText.AppendLine($"{disabledPackagesCount} packages will be disabled:\n".Bold());
            infoText.AppendLine(disabledPackagesList.ToString());
        }

        if(anyStatusChanged)
        {
            filterInfoJss.val = infoText.ToString();
        }
        else
        {
            filterInfoJss.val = "\n".Size(8) + "Nothing was changed.";
        }

        _packagesDirty = _varPackages.Any(package => package.dirty);
        restartVamInfoJss.val = _packagesDirty ? "Restart VAM to reload packages!".Bold().Color(new Color(0.75f, 0, 0)) : "";
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
            /* Nullify static reference fields to let GC collect unreachable instances */
            script = null;
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
