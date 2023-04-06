using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class License
{
    public static License FC { get; } = new License("FC");
    public static License CC_BY { get; } = new License("CC BY");
    public static License CC_BY_SA { get; } = new License("CC BY-SA");
    public static License CC_BY_ND { get; } = new License("CC BY-ND");
    public static License CC_BY_NC { get; } = new License("CC BY-NC");
    public static License CC_BY_NC_SA { get; } = new License("CC BY-NC-SA");
    public static License CC_BY_NC_ND { get; } = new License("CC BY-NC-ND");
    public static License PC { get; } = new License("PC");
    public static License PC_EA { get; } = new License("PC EA");
    public static License Questionable { get; } = new License("Questionable");

    public string name { get; }
    public string displayName { get; }
    public JSONStorableBool enabledJsb { get; }
    public bool isCC { get; }
    public bool requiresAttribution { get; }
    public bool allowsRelicensing { get; }
    public bool allowsCommercialUse { get; }
    public bool allowsDerivatives { get; }

    License(string name)
    {
        this.name = name;
        displayName = name.Replace(" ", "\u00A0");
        enabledJsb = new JSONStorableBool("enabled", true);

        if(name == "FC" || name.StartsWith("CC"))
        {
            isCC = true;
            requiresAttribution = name.Contains("BY");
            allowsRelicensing = !name.Contains("SA");
            allowsCommercialUse = !name.Contains("NC");
            allowsDerivatives = !name.Contains("ND");
        }
        else
        {
            allowsCommercialUse = name == "PC" || name == "PC EA";
        }
    }
}
