static class Loggr
{
    public static void Error(string error) =>
        SuperController.LogError($"{nameof(PackageLicenseFilter)}: {error}. Please report the issue!");

    public static void Message(string message) =>
        SuperController.LogMessage($"{nameof(PackageLicenseFilter)}: {message}");
}
