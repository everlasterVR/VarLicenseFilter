﻿using System.Linq;
using UnityEngine;

sealed class MainWindow : WindowBase
{
    public MainWindow() : base(PackageLicenseFilter.script, nameof(MainWindow))
    {
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
            rectTransform.sizeDelta = new Vector2(-15, 560);
            var textField = fieldTransform.GetComponent<UIDynamicTextField>();
            textField.text = PackageLicenseFilter.script.filterInfoJss.val;
            PackageLicenseFilter.script.AddTextFieldToJss(textField, PackageLicenseFilter.script.filterInfoJss);
            textField.UItext.fontSize = 26;
            textField.backgroundColor = Color.white;
            return textField;
        });
        BuildRightSide();
    }

    void BuildLeftSide(bool rightSide = false)
    {
        var list = PackageLicenseFilter.script.licenseTypes.Values.ToList();
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

        AddSpacer(455, rightSide);

        AddElement(() =>
        {
            var toggle = script.CreateToggle(new JSONStorableBool("Exclude default session plugins", true), rightSide);
            toggle.SetFocusedColor(Colors.lightGray);
            return toggle;
        });

        AddElement(() =>
        {
            var action = PackageLicenseFilter.script.applyFilterAction;
            var button = script.CreateButton(action.name, rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.height = 100;
            action.RegisterButton(button);
            button.SetActiveStyle(!PackageLicenseFilter.script.requireFixAndRestart, true);
            return button;
        });
    }

    void BuildRightSide(bool rightSide = true)
    {
        AddElement(() =>
        {
            var textField = CreateHeaderTextField("\n".Size(12) + "CC auto-selection", 30, 50, rightSide);
            return textField;
        });

        AddElement(() =>
        {
            var button = script.CreateButton("Select all", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.button.onClick.AddListener(() => SelectCCLicenseTypes(license => true));
            return button;
        });
        AddElement(() =>
        {
            var button = script.CreateButton("Allows commercial use and modification", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.button.onClick.AddListener(() => SelectCCLicenseTypes(license => license.allowsCommercialUse && license.allowsDerivatives));
            return button;
        });
        AddElement(() =>
        {
            var button = script.CreateButton("Allows commercial use", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.button.onClick.AddListener(() => SelectCCLicenseTypes(license => license.allowsCommercialUse));
            return button;
        });
        AddElement(() =>
        {
            var button = script.CreateButton("Allows modification", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.button.onClick.AddListener(() => SelectCCLicenseTypes(license => license.allowsDerivatives));
            return button;
        });

        AddSpacer(245, rightSide);

        if(PackageLicenseFilter.script.requireFixAndRestart)
        {
            var action = PackageLicenseFilter.script.fixAndRestartAction;
            AddElement(action.name, () =>
            {
                var button = script.CreateButton(action.name, rightSide);
                button.SetFocusedColor(Colors.lightGray);
                action.RegisterButton(button);
                return button;
            });
        }
        else
        {
            var action = PackageLicenseFilter.script.restartVamAction;
            AddElement(action.name, () =>
            {
                var button = script.CreateButton(action.name, rightSide);
                button.SetFocusedColor(Colors.lightGray);
                action.RegisterButton(button);
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
        var element = GetElement(PackageLicenseFilter.script.restartVamAction.name);
        if(element)
        {
            element.SetActiveStyle(PackageLicenseFilter.script.requireRestart, true);
        }
    }

    delegate bool LicenseFilter(License license);

    static void SelectCCLicenseTypes(LicenseFilter filter)
    {
        var ccLicenseTypes = PackageLicenseFilter.script.licenseTypes.Values.Where(license => license.isCc);
        foreach(var license in ccLicenseTypes)
        {
            license.enabledJsb.val = filter(license);
        }
    }
}
