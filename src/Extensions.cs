using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace everlaster
{
    static partial class ListExtensions
    {
        public static List<JSONStorable> Prune(this List<JSONStorable> list)
        {
            list?.RemoveAll(storable => !storable || !storable.containingAtom);
            return list;
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    static partial class StringExtensions
    {
        public static string Bold(this string str) => $"<b>{str}</b>";
        public static string Italic(this string str) => $"<i>{str}</i>";
        public static string Size(this string str, int size) => $"<size={size}>{str}</size>";
        public static string Color(this string str, string color) => $"<color={color}>{str}</color>";
        public static string Color(this string str, Color color) => str.Color($"#{ColorUtility.ToHtmlStringRGB(color)}");

        public static string ReplaceLastOccurrence(this string str, string oldValue, string newValue)
        {
            int index = str.LastIndexOf(oldValue, StringComparison.Ordinal);
            return index == -1
                ? str
                : str.Remove(index, oldValue.Length).Insert(index, newValue);
        }

        public static int CountOccurrences(this string str, string substring)
        {
            int count = 0;
            int index = 0;

            while ((index = str.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += substring.Length;
            }

            return count;
        }
    }

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
            bool interactable = !(setInteractable && !isActive);
            if(element is UIDynamicSlider)
            {
                var uiDynamicSlider = (UIDynamicSlider) element;
                uiDynamicSlider.slider.interactable = interactable;
                uiDynamicSlider.quickButtonsEnabled = interactable;
                uiDynamicSlider.defaultButtonEnabled = interactable;
                uiDynamicSlider.labelText.color = color;
            }
            else if(element is UIDynamicToggle)
            {
                var uiDynamicToggle = (UIDynamicToggle) element;
                uiDynamicToggle.toggle.interactable = interactable;
                if(highlightIneffective && uiDynamicToggle.toggle.isOn && uiDynamicToggle.toggle.interactable)
                {
                    color = isActive ? Color.black : Color.red;
                }

                uiDynamicToggle.labelText.color = color;
            }
            else if(element is UIDynamicButton)
            {
                var uiDynamicButton = (UIDynamicButton) element;
                uiDynamicButton.button.interactable = interactable;
                var colors = uiDynamicButton.button.colors;
                colors.disabledColor = colors.normalColor;
                uiDynamicButton.button.colors = colors;
                uiDynamicButton.textColor = color;
            }
            else if(element is UIDynamicPopup)
            {
                var uiDynamicPopup = (UIDynamicPopup) element;
                uiDynamicPopup.SetInteractable(interactable);
            }
            else
            {
                throw new ArgumentException($"UIDynamic {element.name} was null, or not an expected type");
            }
        }

        public static void SetFocusedColor(this UIDynamic element, Color color)
        {
            if(!element)
            {
                return;
            }

            if(element is UIDynamicSlider)
            {
                var uiDynamicSlider = (UIDynamicSlider) element;
                var colors = uiDynamicSlider.slider.colors;
                colors.highlightedColor = color;
                colors.pressedColor = color;
                uiDynamicSlider.slider.colors = colors;
            }
            else if(element is UIDynamicToggle)
            {
                var uiDynamicToggle = (UIDynamicToggle) element;
                var colors = uiDynamicToggle.toggle.colors;
                colors.highlightedColor = color;
                colors.pressedColor = color;
                uiDynamicToggle.toggle.colors = colors;
            }
            else if(element is UIDynamicButton)
            {
                var uiDynamicButton = (UIDynamicButton) element;
                var colors = uiDynamicButton.button.colors;
                colors.highlightedColor = color;
                colors.pressedColor = color;
                uiDynamicButton.button.colors = colors;
            }
            else if(element is UIDynamicPopup)
            {
                throw new ArgumentException($"{nameof(UIDynamicPopup)} is not supported");
            }
            else
            {
                throw new ArgumentException($"UIDynamic {element.name} was null, or not an expected type");
            }
        }
    }

    // TODO compare with Naturalis
    static partial class UIDynamicPopupExtensions
    {
        public static void SetInteractable(this UIDynamicPopup popup, bool interactable)
        {
            var slider = popup.GetComponentInChildren<Slider>();
            if(slider)
            {
                slider.interactable = interactable;
            }

            var button = popup.GetComponentInChildren<Button>();
            if(button)
            {
                button.interactable = interactable;
            }
        }
    }
}
