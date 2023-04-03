using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

sealed class Bindings : MonoBehaviour
{
    Dictionary<string, string> _namespace;
    PackageLicenseFilter _script;

    public Dictionary<string, string> Namespace()
    {
        return _namespace;
    }

    public Dictionary<string, JSONStorableAction> actions { get; private set; }

    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public List<object> Actions()
    {
        return actions.Values.Select(action => (object) action).ToList();
    }

    public void Init()
    {
        _script = PackageLicenseFilter.script;
        _namespace = new Dictionary<string, string>
        {
            { "Namespace", nameof(PackageLicenseFilter) },
        };
        var jsonStorableActions = new List<JSONStorableAction>
        {
            _script.NewJSONStorableAction(Constant.OPEN_UI, OpenUI),
        };

        actions = jsonStorableActions.ToDictionary(action => action.name, action => action);
    }

    void OpenUI()
    {
        StartCoroutine(SelectPluginUI());
    }

    // adapted from Timeline v4.3.1 (c) acidbubbles
    IEnumerator SelectPluginUI(Action postAction = null)
    {
        while(_script.initialized == null)
        {
            yield return null;
        }

        if(_script.initialized == false)
        {
            yield break;
        }

        if(_script.UITransform && _script.UITransform.gameObject.activeInHierarchy)
        {
            if(_script.enabled)
            {
                postAction?.Invoke();
            }

            yield break;
        }

        yield return SelectContainingAtomTab("Plugins");

        float timeout = Time.unscaledTime + 1;
        while(Time.unscaledTime < timeout)
        {
            yield return null;

            if(!_script.UITransform)
            {
                continue;
            }

            /* Close any currently open plugin UI before opening this plugin's UI */
            foreach(Transform scriptController in _script.manager.pluginContainer)
            {
                var mvrScript = scriptController.gameObject.GetComponent<MVRScript>();
                if(mvrScript && mvrScript != _script)
                {
                    mvrScript.UITransform.gameObject.SetActive(false);
                }
            }

            if(_script.enabled)
            {
                _script.UITransform.gameObject.SetActive(true);
                postAction?.Invoke();
                yield break;
            }
        }
    }

    IEnumerator SelectContainingAtomTab(string tabName, Action postAction = null)
    {
        if(SuperController.singleton.gameMode != SuperController.GameMode.Edit)
        {
            SuperController.singleton.gameMode = SuperController.GameMode.Edit;
        }

        SuperController.singleton.SelectController(_script.containingAtom.mainController, false, false);
        SuperController.singleton.ShowMainHUDAuto();

        float timeout = Time.unscaledTime + 1;
        UITabSelector selector = null;
        while(Time.unscaledTime < timeout && !selector)
        {
            yield return null;
            selector = _script.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
        }

        if(selector)
        {
            selector.SetActiveTab(tabName);
            postAction?.Invoke();
        }
    }
}
