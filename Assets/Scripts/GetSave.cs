using UnityEngine;
using YG;

public static class GetSave
{
    public static void SaveInt(string key, int value) => YG2.SetState(key, value);
    public static int LoadInt(string key, int def = 0) => YG2.GetState(key);

    public static void SaveFloat(string key, float value) => YG2.SetState(key, Mathf.RoundToInt(value * 100f));
    public static float LoadFloat(string key, float def = 0f) => YG2.GetState(key) / 100f;

    public static void SaveBool(string key, bool value) => YG2.SetState(key, value ? 1 : 0);
    public static bool LoadBool(string key, bool def = false) => YG2.GetState(key) == 1;

    public static void Flush() => YG2.SaveProgress();
}