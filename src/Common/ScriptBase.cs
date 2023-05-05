using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

class ScriptBase : MVRScript
{
    UnityEventsListener PluginUIEventsListener { get; set; }

    // Prevent ScriptBase from showing up as a plugin in Plugins tab
    public override bool ShouldIgnore()
    {
        return true;
    }

#region *** Init UI ***

    public override void InitUI()
    {
        base.InitUI();
        if(!UITransform || PluginUIEventsListener)
        {
            return;
        }

        StartCoroutine(InitUICo());
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual Action OnUIEnabled()
    {
        return null;
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual Action OnUIDisabled()
    {
        return null;
    }

    IEnumerator InitUICo()
    {
        PluginUIEventsListener = UITransform.gameObject.AddComponent<UnityEventsListener>();
        PluginUIEventsListener.OnEnableEvent.AddListener(() => StartCoroutine(OnUIEnabledCo(OnUIEnabled())));

        while(Initialized == null)
        {
            yield return null;
        }

        if(Initialized == false)
        {
            enabledJSON.val = false;
            yield break;
        }

        var onUIDisabled = OnUIDisabled();
        if(onUIDisabled != null)
        {
            PluginUIEventsListener.OnDisableEvent.AddListener(() => StartCoroutine(OnUIDisabledCo(onUIDisabled)));
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

            while(Initialized == null)
            {
                yield return null;
            }

            if(Initialized == false)
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

    public bool? Initialized { get; protected set; }

    protected void FailInitWithMessage(string text)
    {
        Loggr.Message(text);
        Initialized = false;
    }

    protected void FailInitWithError(string text)
    {
        Loggr.Error(text);
        SetGrayBackground();
        CreateErrorTextField(text);
        Initialized = false;
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
        DestroyImmediate(PluginUIEventsListener);
    }

#endregion

#region *** JSON ***

#endregion
}
