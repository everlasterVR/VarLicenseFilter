sealed class VarPackage
{
    public string path { get; }
    public string name { get; }
    public License license { get; }
    public string displayString { get; }

    readonly bool _initialEnabled;
    public bool enabled { get; private set; }
    public bool changed { get; private set; }

    public VarPackage(string path, string name, License license, bool enabled)
    {
        this.path = path;
        this.name = name;
        this.license = license;
        displayString = $"[{license.name}]  {name}";
        _initialEnabled = enabled;
        this.enabled = _initialEnabled;
    }

    public void SyncEnabled()
    {
        enabled = license.enabledJsb.val;
        changed = enabled != _initialEnabled;
    }

    public void Disable()
    {
        enabled = false;
        changed = enabled != _initialEnabled;
    }
}
