using YG;

public static class GetFlags
{
    public static string GetString(string flagName) => YG2.GetFlag(flagName) ?? "";

    public static int GetInt(string flagName, int def = 0)
    {
        return YG2.TryGetFlagAsInt(flagName, out int v) ? v : def;
    }

    public static bool GetBool(string flagName, bool def = false)
    {
        string s = YG2.GetFlag(flagName);
        if (string.IsNullOrEmpty(s)) return def;
        return s.ToLower() == "true" || s == "1";
    }

    public static float GetFloat(string flagName, float def = 0f)
    {
        string s = YG2.GetFlag(flagName);
        return float.TryParse(s, out float v) ? v : def;
    }
}