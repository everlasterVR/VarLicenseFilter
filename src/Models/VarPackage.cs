using everlaster;

sealed class VarPackage
{
    public readonly string path;
    public readonly string filename;
    License _activeLicense;
    readonly License _license;
    SecondaryLicenseInfo _secondaryLicenseInfo;
    public readonly string displayString;

    readonly bool _initialEnabled;
    public bool enabled { get; private set; }
    public bool changed { get; private set; }
    public bool forceEnabled;
    public bool forceDisabled;
    public bool isDefaultSessionPluginPackage;
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
        _license = license;
        _activeLicense = license;
        displayString = $"{filename}\u00A0[{license.displayName}]";
        _initialEnabled = enabled;
        this.enabled = _initialEnabled;
        this.isDefaultSessionPluginPackage = isDefaultSessionPluginPackage;
        this.forceEnabled = forceEnabled;
        this.forceDisabled = forceDisabled;
    }

    public void SetSecondaryLicenseInfo(SecondaryLicenseInfo secondaryLicenseInfo, DateTimeInts today)
    {
        _secondaryLicenseInfo = secondaryLicenseInfo;
        _activeLicense = GetActiveLicense(today);
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
            enabled = applyLicenseFilter ? _activeLicense.enabledJsb.val : _initialEnabled;
        }

        changed = enabled != _initialEnabled;
    }

    /* See MVR.FileManagement.PackageBuilder.SyncDependencyLicenseReport */
    License GetActiveLicense(DateTimeInts today)
    {
        if(_secondaryLicenseInfo == null || _license != License.PC_EA)
        {
            return _license;
        }

        int activeAfterDay = _secondaryLicenseInfo.activeAfterDay;
        int activeAfterMonth = _secondaryLicenseInfo.activeAfterMonth;
        int activeAfterYear = _secondaryLicenseInfo.activeAfterYear;

        if(_secondaryLicenseInfo.ActiveAfterDateIsValidDate())
        {
            if(today.year > _secondaryLicenseInfo.activeAfterYear)
            {
                return _secondaryLicenseInfo.license;
            }

            if(today.year == activeAfterYear)
            {
                if(today.month > activeAfterMonth)
                {
                    return _secondaryLicenseInfo.license;
                }

                if(today.month == activeAfterMonth && today.day > activeAfterDay)
                {
                    return _secondaryLicenseInfo.license;
                }
            }
        }

        return _license;
    }

    public void Disable()
    {
        enabled = false;
        changed = enabled != _initialEnabled;
    }

    public string GetLongDisplayString()
    {
        if(_secondaryLicenseInfo == null || _license != License.PC_EA)
        {
            return $"{filename}\u00A0[<b>{_license.displayName}</b>]";
        }

        if(_activeLicense == _license)
        {
            string primaryLicense = $"<b>{_license.displayName}</b>";
            string secondaryLicense = $"{_secondaryLicenseInfo.license.displayName} after {_secondaryLicenseInfo.GetActiveAfterDateString()}";
            return $"{filename}\u00A0[{primaryLicense}]\u00A0[{secondaryLicense}]";
        }

        if(_activeLicense == _secondaryLicenseInfo.license)
        {
            string primaryLicense = _license.displayName;
            string secondaryLicense = $"<b>{_secondaryLicenseInfo.license.displayName}</b>";
            return $"{filename}\u00A0[{primaryLicense}]\u00A0[{secondaryLicense}]";
        }

        new LogBuilder(filename).Error($"Unexpected active license '{_activeLicense?.name}' on package.");
        return displayString;
    }
}
