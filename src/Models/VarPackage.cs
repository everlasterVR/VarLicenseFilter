sealed class VarPackage
{
    readonly string _path;
    public string name { get; }
    public License license { get; }
    public bool enabled { get; private set; }
    public bool initialEnabled { get; }

    public VarPackage(string path, string name, License license, bool enabled)
    {
        _path = path;
        this.name = name;
        this.license = license;
        this.enabled = enabled;
        initialEnabled = enabled;
    }

    public bool SyncStatus()
    {
        enabled = license.enabledJsb.val;
        if(enabled)
        {
            FileUtils.DeleteDisabledFile(_path);
        }
        else
        {
            FileUtils.CreateDisabledFile(_path);
        }

        return enabled != initialEnabled;
    }
}
