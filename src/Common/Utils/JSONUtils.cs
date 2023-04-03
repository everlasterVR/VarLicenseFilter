using System.Collections.Generic;
using SimpleJSON;

static class JSONUtils
{
    public static Dictionary<string, string> JsonClassToStringDictionary(JSONClass jc)
    {
        var dict = new Dictionary<string, string>();
        if(jc != null)
        {
            foreach(string key in jc.Keys)
            {
                dict[key] = jc[key];
            }
        }

        return dict;
    }

    public static JSONClass StringDictionaryToJsonClass(Dictionary<string, string> dict)
    {
        var jc = new JSONClass();
        if(dict != null)
        {
            foreach(var kvp in dict)
            {
                jc[kvp.Key] = kvp.Value;
            }
        }

        return jc;
    }

    public static void SetStorableValueFromJson(JSONClass jc, JSONStorableParam storable)
    {
        if(!jc.HasKey(storable.name))
        {
            return;
        }

        var jss = storable as JSONStorableString;
        if(jss != null)
        {
            jss.val = jss.defaultVal = jc[jss.name];
            return;
        }

        var jsb = storable as JSONStorableBool;
        if(jsb != null)
        {
            jsb.val = jsb.defaultVal = jc[jsb.name].AsBool;
            return;
        }

        var jssc = storable as JSONStorableStringChooser;
        if(jssc != null)
        {
            jssc.val = jssc.defaultVal = jc[jssc.name];
        }
    }
}
