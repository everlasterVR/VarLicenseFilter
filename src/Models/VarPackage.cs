sealed class VarPackage
{
    public string Path { get; }
    public string Filename { get; }
    License _activeLicense;
    readonly License _license;
    SecondaryLicenseInfo _secondaryLicenseInfo;
    public string DisplayString { get; }

    readonly bool _initialEnabled;
    public bool Enabled { get; private set; }
    public bool Changed { get; private set; }
    public bool ForceEnabled { get; set; }
    public bool ForceDisabled { get; set; }
    public bool IsDefaultSessionPluginPackage { get; set; }

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
        Path = path;
        Filename = filename;
        _license = license;
        _activeLicense = license;
        DisplayString = $"{filename}\u00A0[{license.displayName}]";
        _initialEnabled = enabled;
        Enabled = _initialEnabled;
        IsDefaultSessionPluginPackage = isDefaultSessionPluginPackage;
        ForceEnabled = forceEnabled;
        ForceDisabled = forceDisabled;
    }

    public void SetSecondaryLicenseInfo(SecondaryLicenseInfo secondaryLicenseInfo, DateTimeInts today)
    {
        _secondaryLicenseInfo = secondaryLicenseInfo;
        _activeLicense = GetActiveLicense(today);
    }

    public void SyncEnabled(bool applyLicenseFilter)
    {
        if(ForceEnabled)
        {
            Enabled = true;
        }
        else if(ForceDisabled)
        {
            Enabled = false;
        }
        else
        {
            Enabled = applyLicenseFilter ? _activeLicense.enabledJsb.val : _initialEnabled;
        }

        Changed = Enabled != _initialEnabled;
    }

    /* See MVR.FileManagement.PackageBuilder.SyncDependencyLicenseReport */
    License GetActiveLicense(DateTimeInts today)
    {
        if(_secondaryLicenseInfo == null || _license != License.PC_EA)
        {
            return _license;
        }

        int activeAfterDay = _secondaryLicenseInfo.ActiveAfterDay;
        int activeAfterMonth = _secondaryLicenseInfo.ActiveAfterMonth;
        int activeAfterYear = _secondaryLicenseInfo.ActiveAfterYear;

        if(_secondaryLicenseInfo.ActiveAfterDateIsValidDate())
        {
            if(today.Year > _secondaryLicenseInfo.ActiveAfterYear)
            {
                return _secondaryLicenseInfo.License;
            }

            if(today.Year == activeAfterYear)
            {
                if(today.Month > activeAfterMonth)
                {
                    return _secondaryLicenseInfo.License;
                }

                if(today.Month == activeAfterMonth && today.Day > activeAfterDay)
                {
                    return _secondaryLicenseInfo.License;
                }
            }
        }

        return _license;
    }

    public void Disable()
    {
        Enabled = false;
        Changed = Enabled != _initialEnabled;
    }

    public string GetLongDisplayString()
    {
        if(_secondaryLicenseInfo == null || _license != License.PC_EA)
        {
            return $"{Filename}\u00A0[{_license.displayName.Bold()}]";
        }

        if(_activeLicense == _license)
        {
            string primaryLicense = _license.displayName.Bold();
            string secondaryLicense = $"{_secondaryLicenseInfo.License.displayName} after {_secondaryLicenseInfo.GetActiveAfterDateString()}";
            return $"{Filename}\u00A0[{primaryLicense}]\u00A0[{secondaryLicense}]";
        }

        if(_activeLicense == _secondaryLicenseInfo.License)
        {
            string primaryLicense = _license.displayName;
            string secondaryLicense = _secondaryLicenseInfo.License.displayName.Bold();
            return $"{Filename}\u00A0[{primaryLicense}]\u00A0[{secondaryLicense}]";
        }

        Loggr.Error($"Unexpected active license '{_activeLicense?.name}' on package {Filename}.");
        return DisplayString;
    }
}
