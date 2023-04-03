using System.Text.RegularExpressions;
using MVR.FileManagementSecure;

sealed class VarPackage
{
    public string path { get; }
    public string name { get; }
    public License license { get; }
    public bool enabled { get; private set; }
    public bool initialEnabled { get; }

    readonly string _disabledFilePath;

    public VarPackage(string path, string name, License license, string addonPackagesDirPath)
    {
        this.path = path;
        this.name = name;
        this.license = license;
        enabled = license.enabledJsb.val;
        initialEnabled = enabled;
        _disabledFilePath = Regex.Replace(path, "^AddonPackages/", addonPackagesDirPath) + FileUtils.DISABLED_EXT;
    }

    /* See VarPackage Enabled method*/
    public bool SyncStatus()
    {
        enabled = license.enabledJsb.val;
        if(enabled)
        {
            FileManagerSecure.DeleteFile(_disabledFilePath);
        }
        else
        {
            FileManagerSecure.WriteAllText(_disabledFilePath, string.Empty);
        }

        return enabled != initialEnabled;
    }
}
