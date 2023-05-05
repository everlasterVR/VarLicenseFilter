using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Events;

sealed class UnityEventsListener : MonoBehaviour
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public bool IsEnabled { get; private set; }
    public UnityEvent OnEnableEvent { get; } = new UnityEvent();
    public UnityEvent OnDisableEvent { get; } = new UnityEvent();

    void OnEnable()
    {
        IsEnabled = true;
        OnEnableEvent.Invoke();
    }

    void OnDisable()
    {
        IsEnabled = false;
        OnDisableEvent.Invoke();
    }

    void OnDestroy()
    {
        IsEnabled = false;
        OnEnableEvent.RemoveAllListeners();
        OnDisableEvent.RemoveAllListeners();
    }
}
