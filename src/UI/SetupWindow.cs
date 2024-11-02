using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

sealed class SetupWindow : WindowBase
{
    public SetupWindow() : base(VarLicenseFilter.Script, nameof(SetupWindow))
    {
    }

    protected override void OnBuild()
    {
        AddElement(() =>
        {
            const string text = "Select AddonPackages directory location";
            var jss = new JSONStorableString(text, text);
            var parent = script.UITransform.Find("Scroll View/Viewport/Content");
            var fieldTransform = script.InstantiateTextField(parent);
            UnityEngine.Object.Destroy(fieldTransform.GetComponent<LayoutElement>());
            var rectTransform = fieldTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(10, -100);
            rectTransform.sizeDelta = new Vector2(-20, 50);
            var textField = fieldTransform.GetComponent<UIDynamicTextField>();
            textField.text = jss.val;
            textField.UItext.alignment = TextAnchor.LowerCenter;
            textField.UItext.fontSize = 32;
            textField.height = 60;
            textField.backgroundColor = Color.clear;
            VarLicenseFilter.Script.AddTextFieldToJss(textField, jss);
            return textField;
        });

        var paths = VarLicenseFilter.Script.AddonPackagesDirPaths;
        if(paths.Count > 0)
        {
            BuildSection(paths);
        }
        else
        {
            AddSpacer(100, false);
            AddInfoTextField(
                "No suitable locations found. Please setup the symlink first.",
                false,
                100,
                28
            );
        }

        /* Version text field */
        {
            var versionJss = new JSONStorableString("version", "");
            var versionTextField = CreateVersionTextField(versionJss);
            AddElement(versionTextField);
            VarLicenseFilter.Script.AddTextFieldToJss(versionTextField, versionJss);
        }

        Refresh();
    }

    void BuildSection(List<string> paths)
    {
        for(int i = 0; i < paths.Count; i++)
        {
            string path = paths[i];
            int n = i + 1;
            AddElement(path, () =>
            {
                var parent = script.UITransform.Find("Scroll View/Viewport/Content");
                var buttonTransform = script.InstantiateButton(parent);
                UnityEngine.Object.Destroy(buttonTransform.GetComponent<LayoutElement>());
                var rectTransform = buttonTransform.GetComponent<RectTransform>();
                rectTransform.pivot = new Vector2(0, 0);
                rectTransform.anchoredPosition = new Vector2(10, -100 - 65 * n);
                rectTransform.sizeDelta = new Vector2(-20, 50);
                var button = buttonTransform.GetComponent<UIDynamicButton>();
                button.SetFocusedColor(Colors.lightGray);
                button.buttonText.alignment = TextAnchor.MiddleLeft;
                button.height = 60;
                button.button.onClick.AddListener(() =>
                {
                    VarLicenseFilter.Script.AddonPackagesLocationJss.val = path;
                    Refresh();
                });
                return button;
            });
        }

        var action = VarLicenseFilter.Script.SaveAndContinueAction;
        AddElement(action.name, () =>
        {
            var parent = script.UITransform.Find("Scroll View/Viewport/Content");
            var buttonTransform = script.InstantiateButton(parent);
            UnityEngine.Object.Destroy(buttonTransform.GetComponent<LayoutElement>());
            var rectTransform = buttonTransform.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(275, -100 - (paths.Count + 2) * 65);
            rectTransform.sizeDelta = new Vector2(-550, 50);
            var button = buttonTransform.GetComponent<UIDynamicButton>();
            button.SetFocusedColor(Colors.lightGray);
            button.label = action.name;
            button.SetActiveStyle(false, true);
            action.RegisterButton(button);
            button.height = 60;
            return button;
        });
    }

    void Refresh()
    {
        foreach(string path in VarLicenseFilter.Script.AddonPackagesDirPaths)
        {
            var button = GetElementAs<UIDynamicButton>(path);
            if(button)
            {
                button.label = path == VarLicenseFilter.Script.AddonPackagesLocationJss.val
                    ? "  > ".Bold() + path
                    : "  " + path;
            }
        }

        var saveButton = GetElement(VarLicenseFilter.Script.SaveAndContinueAction.name);
        if(saveButton)
        {
            bool optionIsSelected = !string.IsNullOrEmpty(VarLicenseFilter.Script.AddonPackagesLocationJss.val);
            saveButton.SetActiveStyle(optionIsSelected, true);
        }
    }
}
