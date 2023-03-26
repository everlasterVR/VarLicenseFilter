using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

class ScriptBase : MVRScript
{
    UnityEventsListener pluginUIEventsListener { get; set; }

    public override bool ShouldIgnore() => true; // Prevent ScriptBase from showing up as a plugin in Plugins tab

#region *** Init UI ***

    public override void InitUI()
    {
        base.InitUI();
        if(!UITransform || pluginUIEventsListener)
        {
            return;
        }

        StartCoroutine(InitUICo());
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual Action OnUIEnabled() => null;

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual Action OnUIDisabled() => null;

    IEnumerator InitUICo()
    {
        pluginUIEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
        pluginUIEventsListener.onEnable.AddListener(() => StartCoroutine(OnUIEnabledCo(OnUIEnabled())));

        while(initialized == null)
        {
            yield return null;
        }

        if(initialized == false)
        {
            enabledJSON.val = false;
            yield break;
        }

        var onUIDisabled = OnUIDisabled();
        if(onUIDisabled != null)
        {
            pluginUIEventsListener.onDisable.AddListener(() => StartCoroutine(OnUIDisabledCo(onUIDisabled)));
        }
    }

    bool _inEnabledCo;

    IEnumerator OnUIEnabledCo(Action callback = null)
    {
        if(_inEnabledCo)
        {
            /* When VAM UI is toggled back on with the plugin UI already active, onEnable gets called twice and onDisable once.
             * This ensures onEnable logic executes just once.
             */
            yield break;
        }

        _inEnabledCo = true;
        SetGrayBackground();

        if(callback != null)
        {
            yield return null;
            yield return null;
            yield return null;

            while(initialized == null)
            {
                yield return null;
            }

            if(initialized == false)
            {
                yield break;
            }

            callback();
        }

        _inEnabledCo = false;
    }

    void SetGrayBackground()
    {
        var background = rightUIContent.parent.parent.parent.transform.GetComponent<Image>();
        background.color = Colors.backgroundGray;
    }

    IEnumerator OnUIDisabledCo(Action callback)
    {
        if(_inEnabledCo)
        {
            /* When VAM UI is toggled back on with the plugin UI already active, onEnable gets called twice and onDisable once.
             * This ensures only onEnable logic executes.
             */
            yield break;
        }

        callback();
    }

#endregion

#region *** Init ***

    public bool? initialized { get; protected set; }

    protected void FailInitWithMessage(string text)
    {
        Loggr.Message(text);
        initialized = false;
    }

    protected void FailInitWithError(string text)
    {
        Loggr.Error(text);
        SetGrayBackground();
        CreateErrorTextField(text);
        initialized = false;
    }

    void CreateErrorTextField(string text)
    {
        var errorJss = new JSONStorableString("Error", $"<b>{text}</b>");
        var textField = CreateTextField(errorJss);
        textField.height = Constant.UI_MAX_HEIGHT;
        textField.backgroundColor = Color.clear;
    }

#endregion

#region *** Life cycle ***

    protected void OnDestroy()
    {
        DestroyImmediate(pluginUIEventsListener);
    }

#endregion

#region *** JSON ***

#endregion
}
