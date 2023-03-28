using UnityEngine;

static class Calc
{
    public static float RoundToDecimals(float value, int roundFactor = 1000)
    {
        return Mathf.Round(value * roundFactor) / roundFactor;
    }

    public static float NormalizeFloat(float value, float start, float end)
    {
        return Mathf.Clamp((value - start) / (end - start), 0, 1);
    }
}
