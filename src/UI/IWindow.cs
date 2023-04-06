using System.Diagnostics.CodeAnalysis;

interface IWindow
{
    string GetId();

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    IWindow GetActiveNestedWindow();

    void Rebuild();

    void Clear();

    void ClosePopups();
}
