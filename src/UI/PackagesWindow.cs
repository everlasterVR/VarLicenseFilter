using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

sealed class PackagesWindow : WindowBase
{
    public PackagesWindow(UnityAction onReturnToParent) : base(VarLicenseFilter.script, nameof(PackagesWindow), onReturnToParent)
    {
    }

    protected override void OnBuild()
    {
        AddElement(() =>
        {
            var parent = script.UITransform.Find("Scroll View/Viewport/Content");
            var popupTransform = Utils.DestroyLayout(script.InstantiateFilterablePopup(parent));
            var rectTransform = popupTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(10, -275);
            rectTransform.sizeDelta = new Vector2(-10, 100);
            var popup = popupTransform.GetComponent<UIDynamicPopup>();
            popup.popupPanelHeight = 510;
            VarLicenseFilter.script.AddPopupToJssc(popup, VarLicenseFilter.script.packageJssc);
            popup.popup.onValueChangeHandlers += RefreshToggles;
            return popup;
        });
        AddElement(() =>
        {
            var parent = script.UITransform.Find("Scroll View/Viewport/Content");
            var fieldTransform = Utils.DestroyLayout(script.InstantiateTextField(parent));
            var rectTransform = fieldTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(10, -1220);
            rectTransform.sizeDelta = new Vector2(-15, 425);
            var textField = fieldTransform.GetComponent<UIDynamicTextField>();
            textField.text = VarLicenseFilter.script.filterInfoJss.val;
            VarLicenseFilter.script.AddTextFieldToJss(textField, VarLicenseFilter.script.filterInfoJss);
            textField.UItext.fontSize = 26;
            textField.backgroundColor = Color.white;
            return textField;
        });
        BuildLeftSide();
        BuildRightSide();
        VarLicenseFilter.script.UpdateAlwaysEnabledListInfoText();
        VarLicenseFilter.script.UpdateAlwaysDisabledListInfoText();
        RefreshToggles(VarLicenseFilter.script.packageJssc.val);
    }

    void BuildLeftSide(bool rightSide = false)
    {
        AddInfoTextField("Packages that are always enabled or disabled are ignored when filtering\npackages by license type.", rightSide);

        AddSpacer(80, rightSide);

        AddElement(VarLicenseFilter.script.alwaysEnableSelectedJsb.name, () =>
            script.CreateToggle(VarLicenseFilter.script.alwaysEnableSelectedJsb, rightSide)
        );

        AddElement(() =>
        {
            var textField = CreateHeaderTextField("\n".Size(4) + "Always enabled packages".Bold(), 28, 40, rightSide);
            textField.UItext.alignment = TextAnchor.LowerLeft;
            textField.textColor = Colors.veryDarkGreen;
            return textField;
        });

        AddElement(() =>
        {
            var textField = script.CreateTextField(VarLicenseFilter.script.alwaysEnabledListInfoJss, rightSide);
            textField.height = 376;
            textField.UItext.fontSize = 26;
            textField.UItext.alignment = TextAnchor.UpperLeft;
            textField.UItext.horizontalOverflow = HorizontalWrapMode.Overflow;
            var scrollView = textField.transform.Find("Scroll View");
            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            return textField;
        });
    }

    void BuildRightSide(bool rightSide = true)
    {
        AddSpacer(50, rightSide);

        AddElement(() =>
        {
            var toggle = script.CreateToggle(VarLicenseFilter.script.alwaysEnableDefaultSessionPluginsJsb, rightSide);
            toggle.height = 80;
            toggle.label = "Always enable default session\nplugin packages".Color(Colors.sessionPluginColor);
            return toggle;
        });

        AddSpacer(100, rightSide);

        AddElement(VarLicenseFilter.script.alwaysDisableSelectedJsb.name, () =>
            script.CreateToggle(VarLicenseFilter.script.alwaysDisableSelectedJsb, rightSide)
        );

        AddElement(() =>
        {
            var textField = CreateHeaderTextField("\n".Size(4) + "Always disabled packages".Bold(), 28, 40, rightSide);
            textField.UItext.alignment = TextAnchor.LowerRight;
            textField.textColor = Colors.veryDarkRed;
            return textField;
        });

        AddElement(() =>
        {
            var textField = script.CreateTextField(VarLicenseFilter.script.alwaysDisabledListInfoJss, rightSide);
            textField.height = 376;
            textField.UItext.fontSize = 26;
            textField.UItext.alignment = TextAnchor.UpperLeft;
            textField.UItext.horizontalOverflow = HorizontalWrapMode.Overflow;
            var scrollView = textField.transform.Find("Scroll View");
            var scrollRect = scrollView.GetComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            return textField;
        });
    }

    void RefreshToggles(string packageFileName)
    {
        var enableToggle = GetElementAs<UIDynamicToggle>(VarLicenseFilter.script.alwaysEnableSelectedJsb.name);
        var disableToggle = GetElementAs<UIDynamicToggle>(VarLicenseFilter.script.alwaysDisableSelectedJsb.name);
        if(!enableToggle || !disableToggle)
        {
            Loggr.Error($"Error refreshing toggles. Enable toggle null: {enableToggle == null}, disable toggle null: {disableToggle == null}");
            return;
        }

        bool isSelected = !string.IsNullOrEmpty(packageFileName) && packageFileName != VarLicenseFilter.script.packageJssc.defaultVal;
        enableToggle.SetActiveStyle(isSelected, true);
        disableToggle.SetActiveStyle(isSelected, true);
    }
}
