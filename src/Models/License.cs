using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class License
{
    public static License FC { get; } = new License
    {
        name = "FC",
        allowsRelicensing = true,
        allowsCommercialUse = true,
        allowsDerivatives = true,
        isCc = true,
    };

    public static License CC_BY { get; } = new License
    {
        name = "CC BY",
        allowsRelicensing = true,
        allowsCommercialUse = true,
        allowsDerivatives = true,
        requiresAttribution = true,
        isCc = true,
    };

    public static License CC_BY_SA { get; } = new License
    {
        name = "CC BY-SA",
        allowsCommercialUse = true,
        allowsDerivatives = true,
        requiresAttribution = true,
        isCc = true,
    };

    public static License CC_BY_ND { get; } = new License
    {
        name = "CC BY-ND",
        allowsCommercialUse = true,
        requiresAttribution = true,
        isCc = true,
    };

    public static License CC_BY_NC { get; } = new License
    {
        name = "CC BY-NC",
        allowsRelicensing = true,
        allowsDerivatives = true,
        requiresAttribution = true,
        isCc = true,
    };

    public static License CC_BY_NC_SA { get; } = new License
    {
        name = "CC BY-NC-SA",
        allowsDerivatives = true,
        requiresAttribution = true,
        isCc = true,
    };

    public static License CC_BY_NC_ND { get; } = new License
    {
        name = "CC BY-NC-ND",
        requiresAttribution = true,
        isCc = true,
    };

    public static License PC { get; } = new License
    {
        name = "PC",
    };

    public static License PC_EA { get; } = new License
    {
        name = "PC EA",
    };

    public static License Questionable { get; } = new License
    {
        name = "Questionable",
    };

    public string name { get; private set; }
    public bool isCc { get; private set; }
    public bool requiresAttribution { get; private set; }
    public bool allowsRelicensing { get; private set; }
    public bool allowsCommercialUse { get; private set; }
    public bool allowsDerivatives { get; private set; }
    public JSONStorableBool enabledJsb { get; set; } = new JSONStorableBool("enabled", true);
}
