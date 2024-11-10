#define ENV_DEVELOPMENT
using everlaster.FlatUI;

namespace everlaster
{
    sealed class UIHandler : Handler
    {
        readonly VarLicenseFilter _script;
        MainWindow _mainWindow;
        SetupWindow _setupWindow;

        public UIHandler(VarLicenseFilter script) : base(script)
        {
            _script = script;
        }

        public void GoToMainWindow() => GoToWindow(() =>
        {
            _mainWindow = _mainWindow ?? new MainWindow(_script);
            activeWindow = _mainWindow;
        });

        public void GoToSetupWindow() => GoToWindow(() =>
        {
            _setupWindow = _setupWindow ?? new SetupWindow(_script);
            activeWindow = _setupWindow;
        });
    }
}
