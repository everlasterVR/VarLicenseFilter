using System;
using System.Collections.Generic;

sealed class VarPackage
{
    public string path { get; }
    public string filename { get; }
    public License activeLicense { get; private set; }
    public License license { get; }
    SecondaryLicenseInfo _secondaryLicenseInfo;
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
        activeLicense = license;
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
        activeLicense = GetActiveLicense(today);
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
            enabled = applyLicenseFilter ? activeLicense.enabledJsb.val : _initialEnabled;
        }

        changed = enabled != _initialEnabled;
    }

    /* See MVR.FileManagement.PackageBuilder.SyncDependencyLicenseReport */
    License GetActiveLicense(DateTimeInts today)
    {
        if(_secondaryLicenseInfo == null || license != License.PC_EA)
        {
            return license;
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

        return license;
    }

    public void Disable()
    {
        enabled = false;
        changed = enabled != _initialEnabled;
    }

    public string GetLongDisplayString()
    {
        if(_secondaryLicenseInfo == null || license != License.PC_EA)
        {
            return $"{filename}\u00A0[{license.displayName.Bold()}]";
        }

        if(activeLicense == license)
        {
            string primaryLicense = license.displayName.Bold();
            string secondaryLicense = $"{_secondaryLicenseInfo.license.displayName} after {_secondaryLicenseInfo.GetActiveAfterDateString()}";
            return $"{filename}\u00A0[{primaryLicense}]\u00A0[{secondaryLicense}]";
        }

        if(activeLicense == _secondaryLicenseInfo.license)
        {
            string primaryLicense = license.displayName;
            string secondaryLicense = _secondaryLicenseInfo.license.displayName.Bold();
            return $"{filename}\u00A0[{primaryLicense}]\u00A0[{secondaryLicense}]";
        }

        Loggr.Error($"Unexpected active license '{activeLicense?.name}' on package {filename}.");
        return displayString;
    }
}
