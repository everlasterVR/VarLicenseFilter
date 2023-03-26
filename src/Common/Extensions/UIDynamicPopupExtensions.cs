using UnityEngine.UI;

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
