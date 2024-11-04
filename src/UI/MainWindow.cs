using everlaster.FlatUI;
using everlaster.FlatUI.Elements;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace everlaster
{
    sealed class MainWindow : CustomWindow
    {
        readonly VarLicenseFilter _script;

        public MainWindow(VarLicenseFilter script) : base(script.uiHandler, script.className)
        {
            _script = script;
            AddNestedWindow(new PackagesWindow(script));
        }

        protected override void Build()
        {
            /* Left side */

            {
                var list = _script.licenses.Values.ToList();
                int leftCount = 0;
                int rightCount = 0;
                for(int i = 0; i < list.Count; i++)
                {
                    var license = list[i];
                    if(i < list.Count / 2)
                    {
                        AddToggle(license.name, new Vector2(10, -80 - 60 * leftCount), new Vector2(-820, 50));
                        leftCount++;
                    }
                    else
                    {
                        AddToggle(license.name, new Vector2(270, -80 - 60 * rightCount), new Vector2(-820, 50));
                        rightCount++;
                    }
                }

                AddButton(_script.undoRunFiltersAction, new Vector2(10, -80 - 60 * leftCount - 10), new Vector2(-555, 50))
                    .SetActiveStyle(!_script.requireFixAndRestart, true);
                leftCount++;
                AddButton(_script.applyFilterAction, new Vector2(10, -80 - 60 * leftCount - 60), new Vector2(-555, 100))
                    .SetActiveStyle(_script.requireFixAndRestart, true);
            }

            /* Right side */

            AddHeader("Auto-selection", 545, -85);
            AddButton("Select all", new Vector2(545, -140))
                .SetAlignment(TextAnchor.MiddleLeft)
                .OffsetTextRectX(10)
                .AddListener(() => SelectLicenseTypes(license => true));
            AddButton("Freely distributable (CC)", new Vector2(545, -200))
                .SetAlignment(TextAnchor.MiddleLeft)
                .OffsetTextRectX(10)
                .AddListener(() => SelectLicenseTypes(license => license.isCC));
            AddButton("Allows commercial use (CC)", new Vector2(545, -260))
                .SetAlignment(TextAnchor.MiddleLeft)
                .OffsetTextRectX(10)
                .AddListener(() => SelectLicenseTypes(license => license.isCC && license.allowsCommercialUse));
            AddButton("Allows commercial use (CC) + PC", new Vector2(545, -320))
                .SetAlignment(TextAnchor.MiddleLeft)
                .OffsetTextRectX(10)
                .AddListener(() => SelectLicenseTypes(license => license.allowsCommercialUse));

            var manageButton = AddButton("Manage individual packages", new Vector2(545, -390));
            if(_script.requireFixAndRestart)
            {
                manageButton.SetActiveStyle(false, true);
                AddButton(_script.fixAndRestartAction, new Vector2(545, -500), new Vector2(-555, 100));
            }
            else
            {
                manageButton.AddListener(() => OpenNestedWindow(nameof(PackagesWindow)));
                AddButton(_script.restartVamAction, new Vector2(545, -500), new Vector2(-555, 100))
                    .SetActiveStyle(false, true);
            }

            /* Lower */

            AddTextField(_script.filterInfoString, new Vector2(10, -1230), new Vector2(-15, 715))
                .SetFontSize(26)
                .SetBackgroundColor(Color.white)
                .OffsetTextRectY(-5)
                .SetHorizontalOverflowScroll(false);

            AddVersionInfo();
        }

        protected override void OnRefresh()
        {
            var button = GetButtonElement(_script.restartVamAction);
            if(button != null)
            {
                bool active = _script.requireRestart;
                button.SetActiveStyle(active, true);
                button.SetButtonColor(active ? Colors.buttonRed : Colors.buttonGray);
            }
        }

        delegate bool LicenseFilter(License license);

        void SelectLicenseTypes(LicenseFilter filter)
        {
            foreach(var pair in _script.licenses)
            {
                var license = pair.Value;
                license.enabledJsb.val = filter(license);
            }
        }
    }
}
