struct SecondaryLicenseCacheObject
{
    public string licenseType { get; set; }
    public string activeAfterDay { get; set; }
    public string activeAfterMonth { get; set; }
    public string activeAfterYear { get; set; }

    public override string ToString() =>
        $"{licenseType} active after {activeAfterDay} {activeAfterMonth} {activeAfterYear}";
}
