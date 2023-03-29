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
        AddElement(() =>
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
            textField.backgroundColor = Color.white;
            return textField;
        });
        BuildRightSide();
    }

    void BuildLeftSide(bool rightSide = false)
    {
        var list = PackageLicenseFilter.script.licenseTypeEnabledJsons.Values.ToList();
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
        AddElement(() =>
        {
            // TODO
            var button = script.CreateButton("Allows commercial use only", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.height = 60;
            button.button.onClick.AddListener(SelectAllowsCommercialUseOnly);
            return button;
        });
        AddElement(() =>
        {
            // TODO
            var button = script.CreateButton("Allows modification only", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.height = 60;
            button.button.onClick.AddListener(SelectAllowsDerivativesOnly);
            return button;
        });
        AddElement(() =>
        {
            // TODO
            var button = script.CreateButton("All CC", rightSide);
            button.SetFocusedColor(Colors.lightGray);
            button.height = 60;
            button.button.onClick.AddListener(SelectAllCC);
            return button;
        });
    }

    void BuildRightSide(bool rightSide = true)
    {
        AddElement(() =>
        {
            var action = PackageLicenseFilter.script.applyFilterAction;
            var button = script.CreateButton(action.name.Bold(), rightSide);
            button.SetFocusedColor(Colors.lightGray);
            action.RegisterButton(button);
            return button;
        });

        // TODO
        AddElement(() =>
        {
            var toggle = script.CreateToggle(new JSONStorableBool("Exclude default session plugins", true), rightSide);
            toggle.SetFocusedColor(Colors.lightGray);
            return toggle;
        });

        AddElement(() =>
        {
            var textField = script.CreateTextField(PackageLicenseFilter.script.restartVamInfoJss, rightSide);
            textField.backgroundColor = Color.clear;
            textField.UItext.fontSize = 32;
            var layout = textField.GetComponent<LayoutElement>();
            layout.preferredHeight = 50f;
            layout.minHeight = 50f;
            return textField;
        });

        AddVersionTextField();
    }

    void AddVersionTextField()
    {
        var versionJss = new JSONStorableString("version", "");
        var versionTextField = CreateVersionTextField(versionJss);
        AddElement(versionTextField);
        PackageLicenseFilter.script.AddTextFieldToJss(versionTextField, versionJss);
    }

    void AddLicenseToggle(JSONStorableBool jsb, float posX, float posY)
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
            toggle.label = jsb.name;
            toggle.SetFocusedColor(Colors.lightGray);
            PackageLicenseFilter.script.AddToggleToJsb(toggle, jsb);
            return toggle;
        });
    }

    static void SelectAllowsCommercialUseOnly()
    {
        PackageLicenseFilter.script.licenseTypeEnabledJsons["FC"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-SA"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-ND"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC"].val = false;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC-SA"].val = false;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC-ND"].val = false;
        DisableNonCC();
    }

    static void SelectAllowsDerivativesOnly()
    {
        PackageLicenseFilter.script.licenseTypeEnabledJsons["FC"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-SA"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-ND"].val = false;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC-SA"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC-ND"].val = false;
        DisableNonCC();
    }

    static void SelectAllCC()
    {
        PackageLicenseFilter.script.licenseTypeEnabledJsons["FC"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-SA"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-ND"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC-SA"].val = true;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["CC BY-NC-ND"].val = true;
        DisableNonCC();
    }

    static void DisableNonCC()
    {
        PackageLicenseFilter.script.licenseTypeEnabledJsons["PC"].val = false;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["PC EA"].val = false;
        PackageLicenseFilter.script.licenseTypeEnabledJsons["Questionable"].val = false;
    }
}
