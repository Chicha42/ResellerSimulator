using System;
using YG;

public static class GetServerTime
{
    public static long GetUnixTime() => YG2.ServerTime();

    public static DateTime GetServerDate()
    {
        long unix = YG2.ServerTime();
        if (unix == 0) return DateTime.Now;
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddSeconds(unix)
            .ToLocalTime();
    }

    public static bool IsNewDay(int lastSavedDay)
    {
        return GetServerDate().Day != lastSavedDay;
    }
}