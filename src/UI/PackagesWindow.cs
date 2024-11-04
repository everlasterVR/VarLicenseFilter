using everlaster.FlatUI;
using UnityEngine;

namespace everlaster
{
    sealed class PackagesWindow : CustomWindow
    {
        readonly VarLicenseFilter _script;

        public PackagesWindow(VarLicenseFilter script) : base(script.uiHandler, nameof(PackagesWindow))
        {
            _script = script;
        }

        protected override void Build()
        {
            AddInfo(
                "Packages that are always enabled or disabled are ignored when filtering packages by license type.",
                new Vector2(10, -185),
                new Vector2(-555, 100)
            );

            AddToggle(_script.alwaysEnableDefaultSessionPluginsBool, new Vector2(545, -170), new Vector2(-555, 80));

            AddPopup(PopupType.FILTERABLE, _script.packageJssc, new Vector2(10, -290), new Vector2(-10, 100))
                .Configure(500, -10, true)
                .OnChangeValue(RefreshToggles);

            AddToggle(_script.alwaysEnableSelectedBool, new Vector2(10, -345), new Vector2(-555, 50));
            AddToggle(_script.alwaysDisableSelectedBool, new Vector2(545, -345), new Vector2(-555, 50));

            AddHeader("Always enabled packages", 10, -415).SetAlignment(TextAnchor.LowerLeft).SetTextColor(Colors.darkSuccessColor);
            AddHeader("Always disabled packages", 545, -415).SetAlignment(TextAnchor.LowerRight).SetTextColor(Colors.darkErrorColor);

            AddTextField(_script.alwaysEnabledListInfoString, new Vector2(10, -785), new Vector2(-555, 376))
                .SetFontSize(26)
                .OffsetTextRectY(-5)
                .SetHorizontalOverflowScroll(false);

            AddTextField(_script.alwaysDisabledListInfoString, new Vector2(545, -785), new Vector2(-555, 376))
                .SetFontSize(26)
                .OffsetTextRectY(-5)
                .SetHorizontalOverflowScroll(false);

            AddTextField(_script.filterInfoString, new Vector2(10, -1230), new Vector2(-15, 435))
                .SetFontSize(26)
                .OffsetTextRectY(-5)
                .SetBackgroundColor(Color.white)
                .SetHorizontalOverflowScroll(false);
        }

        protected override void OnPostShow()
        {
            _script.UpdateAlwaysEnabledListInfoText();
            _script.UpdateAlwaysDisabledListInfoText();
            RefreshToggles(_script.packageJssc.val);
        }

        void RefreshToggles(string packageFileName)
        {
            bool isSelected = !string.IsNullOrEmpty(packageFileName) && packageFileName != _script.packageJssc.defaultVal;
            GetToggleElement(_script.alwaysEnableSelectedBool)?.SetActiveStyle(isSelected, true);
            GetToggleElement(_script.alwaysDisableSelectedBool)?.SetActiveStyle(isSelected, true);
        }
    }
}
