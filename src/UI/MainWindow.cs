using UnityEngine;
using UnityEngine.UI;

sealed class MainWindow : WindowBase
{
    public MainWindow() : base(PackageLicenseFilter.script, nameof(MainWindow))
    {
        nestedWindows.Add(new PackagesWindow(OnReturn));
    }

    protected override void OnBuild()
    {
        BuildLeftSide();
        AddElement(() =>
        {
            var parent = script.UITransform.Find("Scroll View/Viewport/Content");
            var fieldTransform = Utils.DestroyLayout(script.InstantiateTextField(parent));
            var rectTransform = fieldTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(10, -1220);
            rectTransform.sizeDelta = new Vector2(-15, 685);
            var textField = fieldTransform.GetComponent<UIDynamicTextField>();
            textField.text = PackageLicenseFilter.script.filterInfoJss.val;
            PackageLicenseFilter.script.AddTextFieldToJss(textField, PackageLicenseFilter.script.filterInfoJss);
            textField.UItext.fontSize = 26;
            textField.backgroundColor = Color.white;
            textField.UItext.horizontalOverflow = HorizontalWrapMode.Overflow;
            var scrollView = textField.transform.Find("Scroll View");
            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            return textField;
        });
        BuildRightSide();
        RefreshRestartButton();
    }

    void BuildLeftSide(bool rightSide = false)
    {
        var list = PackageLicenseFilter.script.licenses.Values.ToList();
        int leftCount = 0;
        int rightCount = 0;
        for(int i = 0; i < list.Count; i++)
        {
            var license = list[i];
            if(i < list.Count / 2)
            {
                AddLicenseToggle(license, 0, -65 * leftCount);
                leftCount++;
            }
            else
            {
                AddLicenseToggle(license, 270, -65 * rightCount);
                rightCount++;
            }
        }

        AddSpacer(330, rightSide);

        AddElement(() =>
        {
            var action = PackageLicenseFilter.script.undoRunFiltersAction;
            var button = script.CreateButton(action.name, rightSide);
            action.RegisterButton(button);
            button.SetFocusedColor(Colors.lightGray);
            button.SetActiveStyle(!PackageLicenseFilter.script.requireFixAndRestart, true);
            return button;
        });

        AddElement(() =>
        {
            var action = PackageLicenseFilter.script.applyFilterAction;
            var button = script.CreateButton(action.name, rightSide);
            action.RegisterButton(button);
            button.height = 100;
            button.SetFocusedColor(Colors.lightGray);
            button.SetActiveStyle(!PackageLicenseFilter.script.requireFixAndRestart, true);
            return button;
        });
    }

    /*
     * Use cases
     * =========
     *
     * 1. Creating a resource to be released for free on the Hub
     *      - Allows free distribution
     * 2. Creating a paid resource
     *      - Allows commercial use
     *      - OR Allows commercial use and free distribution
     */

    void BuildRightSide(bool rightSide = true)
    {
        AddElement(() =>
        {
            var textField = CreateHeaderTextField("\n".Size(12) + "Auto-selection".Bold(), 28, 50, rightSide);
            textField.UItext.alignment = TextAnchor.MiddleLeft;
            return textField;
        });

        AddElement(() =>
        {
            var button = script.CreateButton("  Select all", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.button.onClick.AddListener(() => SelectLicenseTypes(license => true));
            return button;
        });

        AddElement(() =>
        {
            var button = script.CreateButton("  Freely distributable (CC)", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.button.onClick.AddListener(() => SelectLicenseTypes(license => license.isCC));
            return button;
        });

        AddElement(() =>
        {
            var button = script.CreateButton("  Allows commercial use (CC)", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.button.onClick.AddListener(() => SelectLicenseTypes(license => license.isCC && license.allowsCommercialUse));
            return button;
        });

        AddElement(() =>
        {
            var button = script.CreateButton("  Allows commercial use (CC) + PC", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.buttonText.alignment = TextAnchor.MiddleLeft;
            button.button.onClick.AddListener(() => SelectLicenseTypes(license => license.allowsCommercialUse));
            return button;
        });

        AddSpacer(5, rightSide);

        AddElement(() =>
        {
            var button = script.CreateButton("Manage individual packages", rightSide);
            if(PackageLicenseFilter.script.requireFixAndRestart)
            {
                button.SetActiveStyle(false, true);
            }
            else
            {
                var nestedWindow = nestedWindows.Find(window => window.GetId() == nameof(PackagesWindow));
                button.AddListener(() =>
                {
                    ClearSelf();
                    activeNestedWindow = nestedWindow;
                    activeNestedWindow.Rebuild();
                });
            }

            return button;
        });

        if(PackageLicenseFilter.script.requireFixAndRestart)
        {
            var action = PackageLicenseFilter.script.fixAndRestartAction;
            AddElement(action.name, () =>
            {
                var button = script.CreateButton(action.name, rightSide);
                action.RegisterButton(button);
                button.height = 100;
                button.SetFocusedColor(Colors.lightGray);
                return button;
            });
        }
        else
        {
            var action = PackageLicenseFilter.script.restartVamAction;
            AddElement(action.name, () =>
            {
                var button = script.CreateButton(action.name, rightSide);
                action.RegisterButton(button);
                button.height = 100;
                button.SetFocusedColor(Colors.lightGray);
                button.SetActiveStyle(false, true);
                return button;
            });
        }

        AddVersionTextField();
    }

    void AddVersionTextField()
    {
        var versionJss = new JSONStorableString("version", "");
        var versionTextField = CreateVersionTextField(versionJss);
        AddElement(versionTextField);
        PackageLicenseFilter.script.AddTextFieldToJss(versionTextField, versionJss);
    }

    void AddLicenseToggle(License license, float posX, float posY)
    {
        AddElement(() =>
        {
            var parent = script.UITransform.Find("Scroll View/Viewport/Content");
            var toggleTransform = Utils.DestroyLayout(script.InstantiateToggle(parent));
            var rectTransform = toggleTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(10 + posX, -60 + posY);
            rectTransform.sizeDelta = new Vector2(-820, 50);
            var toggle = toggleTransform.GetComponent<UIDynamicToggle>();
            toggle.label = license.name;
            toggle.SetFocusedColor(Colors.lightGray);
            PackageLicenseFilter.script.AddToggleToJsb(toggle, license.enabledJsb);
            return toggle;
        });
    }

    public void RefreshRestartButton()
    {
        var button = GetElementAs<UIDynamicButton>(PackageLicenseFilter.script.restartVamAction.name);
        if(button)
        {
            bool active = PackageLicenseFilter.script.requireRestart;
            button.SetActiveStyle(active, true);
            button.buttonColor = active ? Colors.buttonRed : Colors.buttonGray;
        }
    }

    delegate bool LicenseFilter(License license);

    static void SelectLicenseTypes(LicenseFilter filter)
    {
        foreach(var kvp in PackageLicenseFilter.script.licenses)
        {
            var license = kvp.Value;
            license.enabledJsb.val = filter(license);
        }
    }
}
