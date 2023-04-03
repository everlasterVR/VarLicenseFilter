sealed class VarPackage
{
    public string path { get; }
    public string name { get; }
    public License license { get; }
    public string displayString { get; }
    public bool enabled { get; private set; }
    public bool initialEnabled { get; }

    public VarPackage(string path, string name, License license, bool enabled)
    {
        this.path = path;
        this.name = name;
        this.license = license;
        displayString = $"[{license.name}]  {name}";
        this.enabled = enabled;
        initialEnabled = enabled;
    }

    public bool SyncStatus()
    {
        enabled = license.enabledJsb.val;
        if(enabled)
        {
            FileUtils.DeleteDisabledFile(path);
        }
        else
        {
            FileUtils.CreateDisabledFile(path);
        }

        return enabled != initialEnabled;
    }

    public bool Disable()
    {
        enabled = false;
        FileUtils.CreateDisabledFile(path);
        return enabled != initialEnabled;
    }
}
