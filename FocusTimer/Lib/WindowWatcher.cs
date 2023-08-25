﻿using System;
using System.Linq;
using System.Windows.Threading;
using FocusTimer.Lib.Utility;

namespace FocusTimer.Lib;

public class WindowWatcher
{
    public delegate void FocusedEventHandler(IntPtr prev, IntPtr current);

    public event FocusedEventHandler? OnFocused;

    private readonly DispatcherTimer _timer = new();

    public static readonly string[] SkipList = {
        "TaskListThumbnailWnd", // 윈도우가 여러 개일 때 작업표시줄에 표시되는 작은 썸네일
        "ForegroundStaging", // Alt + Tab으로 보이는 UI
        "MultitaskingViewFrame", // Alt + Tab으로 보이는 UI
        "Windows.UI.Core.CoreWindow", // 시작 버튼 누르면 보이는 UI
        "Shell_TrayWnd", // 작업표시줄
        "Shell_SecondaryTrayWnd", // 다른 모니터에 떠있는 보조 작업표시줄
        "ApplicationManager_DesktopShellWindow", // 새 창 띄울 때에 개입하는 UI
        "WorkerW", // 작업표시줄 오른쪽 눌러서 나오는 바탕화면 UI
        "NotifyIconOverflowWindow", // 작업표시줄에 아이콘이 몰려있는 곳 위로가는 쉐브론 누르면 나오는 UI
    };

    public string? ForegroundAppPath => APIWrapper.GetForegroundProcess()?.ExecutablePath();
    
    private IntPtr _focusedWindow = IntPtr.Zero;
    
    public void StartListening()
    {
        _timer.Tick += (_, _) => Tick();
        _timer.Interval = new TimeSpan(0, 0, 0, 0, 1);

        _timer.Start();
    }

    public void StopListening()
    {
        _timer.Stop();
    }

    private void Tick()
    {
        var nowFocused = APIWrapper.GetForegroundWindow();

        if (nowFocused == IntPtr.Zero)
        {
            // Null 포인터인 핸들은 취급하지 않습니다.
            return;
        }

        if (_focusedWindow == nowFocused)
        {
            // 변화가 없으면 취급하지 않습니다.
            return;
        }

        if (SkipList.Contains(APIWrapper.GetForegroundWindowClass()))
        {
            // 특정 시스템 UI는 다루지 않습니다.
            return;
        }
        
        OnFocused?.Invoke(_focusedWindow, nowFocused);
        
        _focusedWindow = nowFocused;
    }
}