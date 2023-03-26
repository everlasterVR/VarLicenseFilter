interface IWindow
{
    string GetId();

    IWindow GetActiveNestedWindow();

    void Rebuild();

    void Clear();

    void ClosePopups();
}
