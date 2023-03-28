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
        AddElement(
            () =>
            {
                var parent = script.UITransform.Find("Scroll View/Viewport/Content");
                var fieldTransform = Utils.DestroyLayout(script.InstantiateTextField(parent));
                var rectTransform = fieldTransform.GetComponent<RectTransform>();
                rectTransform.pivot = new Vector2(0, 0);
                rectTransform.anchoredPosition = new Vector2(10, -1220);
                rectTransform.sizeDelta = new Vector2(-20, 600);
                var textField = fieldTransform.GetComponent<UIDynamicTextField>();
                textField.text = PackageLicenseFilter.script.filterInfoJss.val;
                PackageLicenseFilter.script.AddTextFieldToJss(textField, PackageLicenseFilter.script.filterInfoJss);
                textField.UItext.fontSize = 26;
                return textField;
            }
        );
        BuildRightSide();
    }

    void BuildLeftSide(bool rightSide = false)
    {
        var list = PackageLicenseFilter.script.licenseTypeEnabledJsons;
        int leftCount = 0;
        int rightCount = 0;
        for(int i = 0; i < list.Count; i++)
        {
            var jsb = list[i];
            if(i < list.Count / 2)
            {
                AddLicenseToggle(jsb, 0, -65 * leftCount);
                leftCount++;
            }
            else
            {
                AddLicenseToggle(jsb, 270, -65 * rightCount);
                rightCount++;
            }
        }

        AddSpacer(300, rightSide);

        AddHeaderTextField("CC auto-selection", rightSide);
        AddElement(
            () =>
            {
                // TODO
                var button = script.CreateButton("Allows commercial use", rightSide);
                button.height = 60;
                return button;
            }
        );
        AddElement(
            () =>
            {
                // TODO
                var button = script.CreateButton("Allows modification", rightSide);
                button.height = 60;
                return button;
            }
        );
        AddElement(
            () =>
            {
                // TODO
                var button = script.CreateButton("All", rightSide);
                button.height = 60;
                return button;
            }
        );
    }

    void BuildRightSide(bool rightSide = true)
    {
        AddElement(
            () =>
            {
                var action = PackageLicenseFilter.script.applyFilterAction;
                var button = script.CreateButton(action.name.Bold(), rightSide);
                action.RegisterButton(button);
                return button;
            }
        );

        // TODO
        AddElement(
            () => script.CreateToggle(new JSONStorableBool("Exclude default session plugins", true), rightSide)
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

        /* Version text field */
        {
            var versionJss = new JSONStorableString("version", "");
            var versionTextField = CreateVersionTextField(versionJss);
            AddElement(versionTextField);
            PackageLicenseFilter.script.AddTextFieldToJss(versionTextField, versionJss);
        }
    }

    void AddLicenseToggle(JSONStorableBool jsb, float posX, float posY)
    {
        AddElement(
            () =>
            {
                var parent = script.UITransform.Find("Scroll View/Viewport/Content");
                var toggleTransform = Utils.DestroyLayout(script.InstantiateToggle(parent));
                var rectTransform = toggleTransform.GetComponent<RectTransform>();
                rectTransform.pivot = new Vector2(0, 0);
                rectTransform.anchoredPosition = new Vector2(10 + posX, -60 + posY);
                rectTransform.sizeDelta = new Vector2(-820, 50);
                var toggle = toggleTransform.GetComponent<UIDynamicToggle>();
                toggle.label = jsb.name;
                PackageLicenseFilter.script.AddToggleToJsb(toggle, jsb);
                return toggle;
            }
        );
    }
}
