using System;
using UnityEngine;
using UnityEngine.Events;

static partial class UIDynamicExtensions
{
    public static void AddListener(this UIDynamic element, UnityAction<bool> callback)
    {
        if(!element)
        {
            return;
        }

        var uiDynamicToggle = element as UIDynamicToggle;
        if(!uiDynamicToggle)
        {
            throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicToggle");
        }

        uiDynamicToggle.toggle.onValueChanged.AddListener(callback);
    }

    public static void AddListener(this UIDynamic element, UnityAction callback)
    {
        if(!element)
        {
            return;
        }

        var uiDynamicButton = element as UIDynamicButton;
        if(!uiDynamicButton)
        {
            throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicButton");
        }

        uiDynamicButton.button.onClick.AddListener(callback);
    }

    public static void AddListener(this UIDynamic element, UnityAction<float> callback)
    {
        if(!element)
        {
            return;
        }

        var uiDynamicSlider = element as UIDynamicSlider;
        if(!uiDynamicSlider)
        {
            throw new ArgumentException($"UIDynamic {element.name} was null or not an UIDynamicSlider");
        }

        uiDynamicSlider.slider.onValueChanged.AddListener(callback);
    }

    public static void SetActiveStyle(this UIDynamic element, bool isActive, bool setInteractable = false, bool highlightIneffective = false)
    {
        if(!element)
        {
            return;
        }

        var color = isActive ? Color.black : Colors.inactive;
        if(element is UIDynamicSlider)
        {
            var uiDynamicSlider = (UIDynamicSlider) element;
            uiDynamicSlider.slider.interactable = !setInteractable || isActive;
            uiDynamicSlider.quickButtonsEnabled = !setInteractable || isActive;
            uiDynamicSlider.defaultButtonEnabled = !setInteractable || isActive;
            uiDynamicSlider.labelText.color = color;
        }
        else if(element is UIDynamicToggle)
        {
            var uiDynamicToggle = (UIDynamicToggle) element;
            uiDynamicToggle.toggle.interactable = !setInteractable || isActive;
            if(highlightIneffective && uiDynamicToggle.toggle.isOn && uiDynamicToggle.toggle.interactable)
            {
                color = isActive ? Color.black : Color.red;
            }

            uiDynamicToggle.labelText.color = color;
        }
        else if(element is UIDynamicButton)
        {
            var uiDynamicButton = (UIDynamicButton) element;
            uiDynamicButton.button.interactable = !setInteractable || isActive;
            var colors = uiDynamicButton.button.colors;
            colors.disabledColor = colors.normalColor;
            uiDynamicButton.button.colors = colors;
            uiDynamicButton.textColor = color;
        }
        else if(element is UIDynamicPopup)
        {
            var uiDynamicPopup = (UIDynamicPopup) element;
            uiDynamicPopup.SetInteractable(!setInteractable || isActive);
        }
        else
        {
            throw new ArgumentException($"UIDynamic {element.name} was null, or not an expected type");
        }
    }
}
