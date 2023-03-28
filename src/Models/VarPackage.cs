using System.Text.RegularExpressions;
using MVR.FileManagementSecure;
using UnityEngine;

sealed class VarPackage
{
    public string path { get; }
    public string name { get; }
    public string license { get; }
    public bool status { get; private set; }
    public bool originalStatus { get; }

    readonly string _disabledFilePath;

    public VarPackage(string path, string name, string license, bool status)
    {
        this.path = path;
        this.name = name;
        this.license = license;
        this.status = status;
        originalStatus = status;
        _disabledFilePath = Regex.Replace(path, "^AddonPackages/", "Custom/PluginData/AddonPackages/") + ".disabled";
    }

    public bool IsDirty() => status != originalStatus;

    /* See VarPackage Enabled method*/
    public void SetStatus(bool value)
    {
        status = value;
        if(status)
        {
            FileManagerSecure.DeleteFile(_disabledFilePath);
        }
        else
        {
            FileManagerSecure.WriteAllText(_disabledFilePath, string.Empty);
        }
    }
}
