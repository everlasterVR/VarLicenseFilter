sealed class VarPackage
{
    public string path { get; }
    public string fileName { get; }
    public License license { get; }
    public string displayString { get; }

    readonly bool _initialEnabled;
    public bool enabled { get; private set; }
    public bool changed { get; private set; }

    public bool forceDisabled { get; set; }
    public bool forceEnabled { get; set; }

    public VarPackage(string path, string fileName, License license, bool enabled)
    {
        this.path = path;
        this.fileName = fileName;
        this.license = license;
        displayString = $"[{license.name}] {fileName}";
        _initialEnabled = enabled;
        this.enabled = _initialEnabled;
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
