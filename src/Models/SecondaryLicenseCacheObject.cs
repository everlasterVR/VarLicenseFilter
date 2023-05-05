struct SecondaryLicenseCacheObject
{
    public string LicenseType { get; set; }
    public string ActiveAfterDay { get; set; }
    public string ActiveAfterMonth { get; set; }
    public string ActiveAfterYear { get; set; }

    public override string ToString()
    {
        return $"{LicenseType} active after {ActiveAfterDay} {ActiveAfterMonth} {ActiveAfterYear}";
    }
}
