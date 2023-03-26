using System.Linq;
using UnityEngine;
using UnityEngine.UI;

sealed class MainWindow : WindowBase
{
    public MainWindow() : base(PackageLicenseFilter.script, nameof(MainWindow))
    {
    }

    protected override void OnBuild()
    {
        BuildLeftSide();
        BuildRightSide();

        AddElement(
            () =>
            {
                var parent = script.UITransform.Find("Scroll View/Viewport/Content");
                var fieldTransform = Utils.DestroyLayout(script.InstantiateTextField(parent));
                var rectTransform = fieldTransform.GetComponent<RectTransform>();
                rectTransform.pivot = new Vector2(0, 0);
                rectTransform.anchoredPosition = new Vector2(10, -1220);
                rectTransform.sizeDelta = new Vector2(-20, 560);
                var textField = fieldTransform.GetComponent<UIDynamicTextField>();
                textField.text = PackageLicenseFilter.script.filterInfoJss.val;
                PackageLicenseFilter.script.AddTextFieldToJss(textField, PackageLicenseFilter.script.filterInfoJss);
                textField.UItext.fontSize = 26;
                return textField;
            }
        );
    }

    void BuildLeftSide(bool rightSide = false)
    {
        foreach(var jsb in PackageLicenseFilter.script.licenseTypeEnabledJsons)
        {
            AddElement(
                () => script.CreateToggle(jsb, rightSide)
            );
        }

    }

    void BuildRightSide(bool rightSide = true)
    {
        AddElement(
            () =>
            {
                var action = PackageLicenseFilter.script.applyFilterAction;
                var button = script.CreateButton(action.name, rightSide);
                action.RegisterButton(button);
                return button;
            }
        );

        AddElement(
            () =>
            {
                var textField = script.CreateTextField(PackageLicenseFilter.script.restartVamInfoJss, rightSide);
                textField.backgroundColor = Color.clear;
                textField.UItext.fontSize = 32;
                var layout = textField.GetComponent<LayoutElement>();
                layout.preferredHeight = 50f;
                layout.minHeight = 50f;
                return textField;
            }
        );

        // TODO select CC
        // TODO select permits commercial use
        // TODO select permits modification
        // TODO select all

        /* Version text field */
        {
            var versionJss = new JSONStorableString("version", "");
            var versionTextField = CreateVersionTextField(versionJss);
            AddElement(versionTextField);
            PackageLicenseFilter.script.AddTextFieldToJss(versionTextField, versionJss);
        }
    }
}
