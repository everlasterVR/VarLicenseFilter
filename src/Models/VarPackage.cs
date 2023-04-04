sealed class VarPackage
{
    public string path { get; }
    public string filename { get; }
    public License license { get; }
    public string displayString { get; }

    readonly bool _initialEnabled;
    public bool enabled { get; private set; }
    public bool changed { get; private set; }
    public bool forceEnabled { get; set; }
    public bool forceDisabled { get; set; }
    public bool isDefaultSessionPluginPackage { get; set; }

    public VarPackage(
        string path,
        string filename,
        License license,
        bool enabled,
        bool isDefaultSessionPluginPackage,
        bool forceEnabled,
        bool forceDisabled
    )
    {
        this.path = path;
        this.filename = filename;
        this.license = license;
        displayString = $"{filename}\u00A0[{license.displayName}]";
        _initialEnabled = enabled;
        this.enabled = _initialEnabled;
        this.isDefaultSessionPluginPackage = isDefaultSessionPluginPackage;
        this.forceEnabled = forceEnabled;
        this.forceDisabled = forceDisabled;
    }

    public void SyncEnabled(bool applyLicenseFilter)
    {
        if(forceEnabled)
        {
            enabled = true;
        }
        else if(forceDisabled)
        {
            enabled = false;
        }
        else
        {
            enabled = applyLicenseFilter ? license.enabledJsb.val : _initialEnabled;
        }

        changed = enabled != _initialEnabled;
    }

    public void Disable()
    {
        enabled = false;
        changed = enabled != _initialEnabled;
    }
}
