using everlaster.FlatUI;
using UnityEngine;

namespace everlaster
{
    sealed class SetupWindow : CustomWindow
    {
        readonly VarLicenseFilter _script;

        public SetupWindow(VarLicenseFilter script) : base(script.uiHandler, nameof(SetupWindow))
        {
            _script = script;
        }

        const string SAVE_AND_CONTINUE = "Save selected location and continue";

        protected override void Build()
        {
            AddTextField("Select AddonPackages directory location", new Vector2(10, -90), new Vector2(-20, 50))
                .SetFontSize(30)
                .SetAlignment(TextAnchor.MiddleCenter)
                .SetBackgroundColor(Color.clear)
                .DisableScroll();

            var paths = _script.addonPackagesDirPaths;
            if(paths.Count > 0)
            {
                for(int i = 0; i < paths.Count; i++)
                {
                    int n = i + 1;
                    string path = paths[i];
                    AddButton(path, new Vector2(10, -100 - 65 * n), new Vector2(-20, 60))
                        .SetAlignment(TextAnchor.MiddleLeft)
                        .OffsetTextRectX(10)
                        .AddListener(() =>
                        {
                            _script.addonPackagesLocationString.val = path;
                            Refresh();
                        });
                }

                AddButton(SAVE_AND_CONTINUE, new Vector2(275, -100 - (paths.Count + 2) * 65), new Vector2(-550, 60))
                    .AddListener(_script.SaveAndContinue)
                    .SetActiveStyle(false, true);
            }
            else
            {
                AddInfo(
                    "No suitable locations found. Please setup the symlink first.",
                    new Vector2(10, -165),
                    new Vector2(-20, 50)
                );
            }

            AddVersionInfo();
        }

        protected override void OnRefresh()
        {
            foreach(string path in _script.addonPackagesDirPaths)
            {
                GetButtonElement(path)?.SetText(path == _script.addonPackagesLocationString.val ? $"<b>></b> {path}" : path);
            }

            GetButtonElement(SAVE_AND_CONTINUE)?.SetActiveStyle(!string.IsNullOrEmpty(_script.addonPackagesLocationString.val), true);
        }
    }
}
