static partial class JSONStorableExtensions
{
    public static bool IsEnabledNullSafe(this JSONStorable storable) =>
        storable && storable.enabled;

    public static void CallActionNullSafe(this JSONStorable storable, string actionName)
    {
        if(storable && storable.IsAction(actionName))
        {
            storable.CallAction(actionName);
        }
    }

    public static bool GetBoolParamValueNullSafe(this JSONStorable storable, string paramName)
    {
        if(storable && storable.IsBoolJSONParam(paramName))
        {
            return storable.GetBoolParamValue(paramName);
        }

        return false;
    }

    public static void SetStringParamValueNullSafe(this JSONStorable storable, string paramName, string value)
    {
        if(storable && storable.IsStringJSONParam(paramName))
        {
            storable.SetStringParamValue(paramName, value);
        }
    }
}
