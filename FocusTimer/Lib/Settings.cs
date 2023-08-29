﻿namespace FocusTimer.Lib;

/// <summary>
/// 애플리케이션의 설정에 접근할 수 있는 인터페이스를 제공합니다.
/// </summary>
public static class Settings
{
    public static int GetFocusLockHoldDuration()
    {
        var got = Properties.Settings.Default.FocusLockHoldDuration;
        const int fallback = 10;

        return got <= 0 ? fallback : got;
    }

    public static void SetFocusLockHoldDuration(int duration)
    {
        Properties.Settings.Default.FocusLockHoldDuration = duration;

        Properties.Settings.Default.Save();
    }

    public static int GetActivityTimeout()
    {
        var got = Properties.Settings.Default.ActivityTimeout;
        const int fallback = 10;

        return got <= 0 ? fallback : got;
    }

    public static void SetActivityTimeout(int timeout)
    {
        Properties.Settings.Default.ActivityTimeout = timeout;

        Properties.Settings.Default.Save();
    }
}