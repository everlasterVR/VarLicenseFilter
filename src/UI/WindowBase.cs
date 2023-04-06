using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

class WindowBase : IWindow
{
    readonly Dictionary<string, UIDynamic> _elements;
    readonly string _id;

    readonly UnityAction _onReturnToParent;
    protected readonly List<IWindow> nestedWindows;
    protected readonly MVRScript script;

    protected IWindow activeNestedWindow;

    protected WindowBase(MVRScript script, string id, UnityAction onReturnToParent = null)
    {
        this.script = script;
        _id = id;
        _onReturnToParent = onReturnToParent;
        _elements = new Dictionary<string, UIDynamic>();
        nestedWindows = new List<IWindow>();
    }

    public string GetId()
    {
        return _id;
    }

    public IWindow GetActiveNestedWindow()
    {
        return activeNestedWindow;
    }

    public void Rebuild()
    {
        if(activeNestedWindow != null)
        {
            activeNestedWindow.Rebuild();
        }
        else
        {
            _elements.Clear();
            if(_onReturnToParent != null)
            {
                AddBackButton(false, _onReturnToParent);
            }

            OnBuild();
        }
    }

    public void ClosePopups()
    {
        if(activeNestedWindow != null)
        {
            activeNestedWindow.ClosePopups();
        }
        else
        {
            ClosePopupsSelf();
        }
    }

    public void Clear()
    {
        if(activeNestedWindow != null)
        {
            activeNestedWindow.Clear();
        }
        else
        {
            ClearSelf();
        }

        OnClose();
    }

    protected virtual void OnBuild()
    {
    }

    [SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
    protected virtual void OnClose()
    {
    }

    [SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
    public void OnReturn()
    {
        activeNestedWindow.Clear();
        activeNestedWindow = null;
        if(_onReturnToParent != null)
        {
            AddBackButton(false, _onReturnToParent);
        }

        OnBuild();
    }

    protected UIDynamic GetElement(string key)
    {
        return _elements.ContainsKey(key) ? _elements[key] : null;
    }

    protected T GetElementAs<T>(string key)
    {
        if(_elements.ContainsKey(key))
        {
            var element = _elements[key];
            if(element is T)
            {
                return (T) Convert.ChangeType(element, typeof(T));
            }
        }

        return default(T);
    }

    protected void ClearSelf()
    {
        ClosePopupsSelf();
        foreach(var element in _elements.ToList())
        {
            script.RemoveElement(element.Value);
        }
    }

    void ClosePopupsSelf()
    {
        foreach(var element in _elements)
        {
            var uiDynamicPopup = element.Value as UIDynamicPopup;
            if(uiDynamicPopup)
            {
                uiDynamicPopup.popup.visible = false;
            }
        }
    }

#region *** Common Elements ***

    protected void AddSpacer(int height, bool rightSide)
    {
        AddElement(() => script.NewSpacer(height, rightSide));
    }

    protected void AddElement(Func<UIDynamic> createElement)
    {
        AddElement(Guid.NewGuid().ToString(), createElement);
    }

    protected void AddElement(string key, Func<UIDynamic> createElement)
    {
        _elements[key] = createElement();
    }

    protected void AddElement(UIDynamic element)
    {
        _elements[Guid.NewGuid().ToString()] = element;
    }

    void AddBackButton(bool rightSide, UnityAction onReturnToParent)
    {
        AddElement(() =>
        {
            var button = script.CreateButton("Return", rightSide);
            button.textColor = Color.white;
            var colors = button.button.colors;
            colors.normalColor = Colors.sliderGray;
            colors.highlightedColor = Color.grey;
            colors.pressedColor = Color.grey;
            button.button.colors = colors;
            button.AddListener(onReturnToParent);
            return button;
        });
    }

    UIDynamicTextField CreateBasicTextField(string text, bool rightSide)
    {
        return script.CreateTextField(new JSONStorableString("text", text), rightSide);
    }

    protected UIDynamicTextField CreateHeaderTextField(
        string text,
        int fontSize,
        int height,
        bool rightSide
    )
    {
        var textField = CreateBasicTextField(text, rightSide);
        ModifyTextField(textField, fontSize, height);
        textField.UItext.alignment = TextAnchor.MiddleCenter;
        return textField;
    }

    static void ModifyTextField(UIDynamicTextField textField, int fontSize, int height)
    {
        textField.UItext.fontSize = fontSize;
        textField.backgroundColor = Color.clear;
        var layout = textField.GetComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
    }

    protected void AddInfoTextField(string text, bool rightSide, int height = 100, int fontSize = 26)
    {
        AddElement(() =>
        {
            var textField = CreateBasicTextField(text, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.backgroundColor = Color.clear;
            var layout = textField.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            return textField;
        });
    }

    protected UIDynamicTextField CreateVersionTextField(JSONStorableString jss)
    {
        var parent = script.UITransform.Find("Scroll View/Viewport/Content");
        var fieldTransform = Utils.DestroyLayout(script.InstantiateTextField(parent));
        var rectTransform = fieldTransform.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector2(530, -Constant.UI_MAX_HEIGHT - 27);
        rectTransform.sizeDelta = new Vector2(-556, 42);
        jss.val = $"v{VarLicenseFilter.VERSION}";
        var textField = fieldTransform.GetComponent<UIDynamicTextField>();
        textField.text = jss.val;
        textField.backgroundColor = Color.clear;
        textField.UItext.alignment = TextAnchor.LowerRight;
        textField.UItext.fontSize = 26;
        return textField;
    }

#endregion
}
