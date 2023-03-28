using SimpleJSON;

static class JSONUtils
{
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
