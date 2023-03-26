using UnityEngine;
using UnityEngine.Events;

sealed class UnityEventsListener : MonoBehaviour
{
    public bool isEnabled { get; private set; }
    public readonly UnityEvent onEnable = new UnityEvent();
    public readonly UnityEvent onDisable = new UnityEvent();

    void OnEnable()
    {
        isEnabled = true;
        onEnable.Invoke();
    }

    void OnDisable()
    {
        isEnabled = false;
        onDisable.Invoke();
    }

    void OnDestroy()
    {
        isEnabled = false;
        onEnable.RemoveAllListeners();
        onDisable.RemoveAllListeners();
    }
}
