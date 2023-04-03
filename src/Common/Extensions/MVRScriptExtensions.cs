// ReSharper disable MemberCanBePrivate.Global UnusedMember.Global UnusedMethodReturnValue.Global UnusedType.Global
using System;
using UnityEngine;

static partial class MVRScriptExtensions
{
    public static string GetPackagePath(this MVRScript script)
    {
        string packageId = script.GetPackageId();
        return packageId == null ? "" : $"{packageId}:/";
    }

    public static string GetPackageName(this MVRScript script)
    {
        string packageId = script.GetPackageId();
        return packageId == null ? "" : packageId.Substring(0, packageId.LastIndexOf('.'));
    }

    //MacGruber / Discord 20.10.2020
    //Get path prefix of the package that contains this plugin
    public static string GetPackageId(this MVRScript script)
    {
        string id = script.name.Substring(0, script.name.IndexOf('_'));
        string filename = script.manager.GetJSON()["plugins"][id].Value;
        int idx = filename.IndexOf(":/", StringComparison.Ordinal);
        return idx >= 0 ? filename.Substring(0, idx) : null;
    }

    public static Transform InstantiateTextField(this MVRScript script, Transform parent = null)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableTextFieldPrefab, parent, false);
    }

    public static Transform InstantiateButton(this MVRScript script, Transform parent = null)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableButtonPrefab, parent, false);
    }

    public static Transform InstantiateSlider(this MVRScript script, Transform parent = null)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableSliderPrefab, parent, false);
    }

    public static Transform InstantiateToggle(this MVRScript script, Transform parent = null)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableTogglePrefab, parent, false);
    }

    public static Transform InstantiatePopup(this MVRScript script, Transform parent = null)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurablePopupPrefab, parent, false);
    }

    public static Transform InstantiateColorPicker(this MVRScript script, Transform parent = null)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableColorPickerPrefab, parent, false);
    }

    public static Transform InstantiateSpacer(this MVRScript script, Transform parent = null)
    {
        return UnityEngine.Object.Instantiate(script.manager.configurableSpacerPrefab, parent, false);
    }

    public static JSONStorableString NewJSONStorableString(
        this MVRScript script,
        string paramName,
        string startingValue,
        bool shouldRegister = true
    )
    {
        var storable = new JSONStorableString(paramName, startingValue)
        {
            storeType = JSONStorableParam.StoreType.Full,
        };
        if(shouldRegister)
        {
            script.RegisterString(storable);
        }

        return storable;
    }

    public static JSONStorableBool NewJSONStorableBool(
        this MVRScript script,
        string paramName,
        bool startingValue,
        bool shouldRegister = true
    )
    {
        var storable = new JSONStorableBool(paramName, startingValue)
        {
            storeType = JSONStorableParam.StoreType.Full,
        };
        if(shouldRegister)
        {
            script.RegisterBool(storable);
        }

        return storable;
    }

    public static JSONStorableFloat NewJSONStorableFloat(
        this MVRScript script,
        string paramName,
        float startingValue,
        float minValue,
        float maxValue,
        bool shouldRegister = true
    )
    {
        var storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue)
        {
            storeType = JSONStorableParam.StoreType.Full,
        };
        if(shouldRegister)
        {
            script.RegisterFloat(storable);
        }

        return storable;
    }

    public static JSONStorableAction NewJSONStorableAction(
        this MVRScript script,
        string paramName,
        JSONStorableAction.ActionCallback callback,
        bool shouldRegister = true
    )
    {
        var storable = new JSONStorableAction(paramName, callback);
        if(shouldRegister)
        {
            script.RegisterAction(storable);
        }

        return storable;
    }

    public static UIDynamic NewSpacer(
        this MVRScript script,
        float height,
        bool rightSide = false
    )
    {
        if(height <= 0)
        {
            return null;
        }

        var spacer = script.CreateSpacer(rightSide);
        spacer.height = height;
        return spacer;
    }

    public static void RemoveElement(this MVRScript script, UIDynamic element)
    {
        if(element is UIDynamicTextField)
        {
            script.RemoveTextField((UIDynamicTextField) element);
        }
        else if(element is UIDynamicButton)
        {
            script.RemoveButton((UIDynamicButton) element);
        }
        else if(element is UIDynamicSlider)
        {
            script.RemoveSlider((UIDynamicSlider) element);

        }
        else if(element is UIDynamicToggle)
        {
            script.RemoveToggle((UIDynamicToggle) element);

        }
        else if(element is UIDynamicPopup)
        {
            script.RemovePopup((UIDynamicPopup) element);

        }
        else if(element is UIDynamicColorPicker)
        {
            script.RemoveColorPicker((UIDynamicColorPicker) element);
        }
        else
        {
            script.RemoveSpacer(element);
        }
    }
}
