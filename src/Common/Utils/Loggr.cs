static class Loggr
{
    public static void Error(string error, bool report = true)
    {
        SuperController.LogError($"{nameof(PackageLicenseFilter)}: {error}.{(report ? " Please report the issue!" : "")}");
    }

    public static void Message(string message)
    {
        SuperController.LogMessage($"{nameof(PackageLicenseFilter)}: {message}");
    }
}
