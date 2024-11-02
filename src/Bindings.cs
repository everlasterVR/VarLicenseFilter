using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class Bindings : MonoBehaviour
{
    ScriptBase _script;
    public Dictionary<string, string> Namespace { get; private set; }
    Dictionary<string, JSONStorableAction> _actions;

    public void Init(ScriptBase script, string namespaceName)
    {
        _script = script;
        Namespace = new Dictionary<string, string>
        {
            { "Namespace", namespaceName },
        };
        _actions = new Dictionary<string, JSONStorableAction>();
        AddActions(new List<JSONStorableAction>
        {
            script.NewJSONStorableAction("OpenUI", () => StartCoroutine(SelectPluginUI())),
        });
    }

    public IEnumerable<object> GetActionsList()
    {
        return _actions.Values.Select(action => (object) action).ToList();
    }

    void AddActions(List<JSONStorableAction> actionsList)
    {
        foreach(var action in actionsList)
        {
            _actions[action.name] = action;
        }
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
        while(Time.unscaledTime < timeout)
        {
            yield return null;
            var selector = _script.containingAtom.gameObject.GetComponentInChildren<UITabSelector>();
            if(selector)
            {
                selector.SetActiveTab(tabName);
                postAction?.Invoke();
                break;
            }
        }
    }
}
