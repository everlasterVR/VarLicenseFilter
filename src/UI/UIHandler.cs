#define ENV_DEVELOPMENT
using everlaster.FlatUI;
using System;

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

        void GoToWindow(Action callback)
        {
            activeWindow?.Hide();
            elementsParent = script.UITransform;
            callback();
            if(activeWindow == null)
            {
                throw new NullReferenceException("GoToWindow: active window is null");
            }

            activeWindow.Show();

            #if ENV_DEVELOPMENT
            {
                ToggleDevSection(true);
                UpdateDevRectOptions();
            }
            #endif
        }
    }
}
