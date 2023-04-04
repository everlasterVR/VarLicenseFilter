using UnityEngine;
using UnityEngine.Events;

sealed class PackagesWindow : WindowBase
{
    public PackagesWindow(UnityAction onReturnToParent) : base(PackageLicenseFilter.script, nameof(PackagesWindow), onReturnToParent)
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
            PackageLicenseFilter.script.AddPopupToJssc(popup, PackageLicenseFilter.script.packageJssc);
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
            textField.text = PackageLicenseFilter.script.filterInfoJss.val;
            PackageLicenseFilter.script.AddTextFieldToJss(textField, PackageLicenseFilter.script.filterInfoJss);
            textField.UItext.fontSize = 26;
            textField.backgroundColor = Color.white;
            return textField;
        });
        BuildLeftSide();
        BuildRightSide();
        PackageLicenseFilter.script.UpdateAlwaysEnabledListInfoText();
        PackageLicenseFilter.script.UpdateAlwaysDisabledListInfoText();
        RefreshToggles(PackageLicenseFilter.script.packageJssc.val);
    }

    void BuildLeftSide(bool rightSide = false)
    {
        AddInfoTextField("Packages that are always enabled or disabled are ignored when filtering\npackages by license type.", rightSide);
        AddSpacer(80, rightSide);
        var alwaysEnableJsb = PackageLicenseFilter.script.alwaysEnableSelectedJsb;
        AddElement(alwaysEnableJsb.name, () =>
        {
            var toggle = script.CreateToggle(alwaysEnableJsb, rightSide);
            return toggle;
        });
        AddElement(() =>
        {
            var textField = CreateHeaderTextField("\n".Size(8) + "ALWAYS ENABLED PACKAGES".Bold(), 26, 40, rightSide);
            textField.UItext.alignment = TextAnchor.LowerLeft;
            textField.textColor = new Color(0, 0.33f, 0);
            return textField;
        });
        AddElement(() =>
        {
            var textField = script.CreateTextField(PackageLicenseFilter.script.alwaysEnabledListInfoJss, rightSide);
            textField.height = 376;
            textField.UItext.fontSize = 26;
            return textField;
        });
    }

    void BuildRightSide(bool rightSide = true)
    {
        AddSpacer(50, rightSide);
        AddElement(() =>
        {
            var toggle = script.CreateToggle(PackageLicenseFilter.script.alwaysEnableDefaultSessionPluginsJsb, rightSide);
            toggle.height = 80;
            toggle.label = "Always enable default session\nplugin packages";
            return toggle;
        });
        AddSpacer(100, rightSide);
        var alwaysDisableJsb = PackageLicenseFilter.script.alwaysDisableSelectedJsb;
        AddElement(alwaysDisableJsb.name, () =>
        {
            var toggle = script.CreateToggle(alwaysDisableJsb, rightSide);
            return toggle;
        });
        AddElement(() =>
        {
            var textField = CreateHeaderTextField("\n".Size(8) + "ALWAYS DISABLED PACKAGES".Bold(), 26, 40, rightSide);
            textField.UItext.alignment = TextAnchor.LowerLeft;
            textField.textColor = new Color(0.33f, 0, 0);
            return textField;
        });
        AddElement(() =>
        {
            var textField = script.CreateTextField(PackageLicenseFilter.script.alwaysDisabledListInfoJss, rightSide);
            textField.height = 376;
            textField.UItext.alignment = TextAnchor.LowerLeft;
            textField.UItext.fontSize = 26;
            return textField;
        });
    }

    void RefreshToggles(string packageFileName)
    {
        var enableToggle = GetElementAs<UIDynamicToggle>(PackageLicenseFilter.script.alwaysEnableSelectedJsb.name);
        var disableToggle = GetElementAs<UIDynamicToggle>(PackageLicenseFilter.script.alwaysDisableSelectedJsb.name);
        if(!enableToggle || !disableToggle)
        {
            Loggr.Error($"Error refreshing toggles. Enable toggle null: {enableToggle == null}, disable toggle null: {disableToggle == null}");
            return;
        }

        bool isSelected = !string.IsNullOrEmpty(packageFileName) && packageFileName != PackageLicenseFilter.script.packageJssc.defaultVal;
        enableToggle.SetActiveStyle(isSelected, true);
        disableToggle.SetActiveStyle(isSelected, true);
    }
}
