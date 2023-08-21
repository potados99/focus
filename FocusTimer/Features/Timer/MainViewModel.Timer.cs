﻿using FocusTimer.Lib;
using FocusTimer.Lib.Component;
using FocusTimer.Lib.Utility;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FocusTimer.Data.Repositories;
using FocusTimer.Domain.Entities;
using FocusTimer.Domain.Services;
using FocusTimer.Features.License;
using FocusTimer.Features.Timer.Slot;

namespace FocusTimer.Features.Timer;

internal partial class MainViewModel : BaseModel
{
    #region 스탑워치와 글로벌 틱 타이머

    public void InitGlobalTimer()
    {
        OneSecTickTimer.Stop();
        OneSecTickTimer.RemoveHandlers();
        AlwaysOnStopwatch.Reset();
        ActiveStopwatch.Reset();

        OneSecTickTimer.Tick += (_, _) =>
        {
            TickAll();
            RenderAll();
        };
        OneSecTickTimer.Interval = TimeSpan.FromSeconds(1);
        OneSecTickTimer.Start();

        AlwaysOnStopwatch.Start();
        ActiveStopwatch.Start();
    }

    private readonly Stopwatch ActiveStopwatch = new();
    private readonly Stopwatch AlwaysOnStopwatch = new();
    private readonly DispatcherTimer OneSecTickTimer = new();

    #endregion

    #region 포커스 잠금과 홀드 타이머

    public void InitFocusLock()
    {
        FocusLockTimer.OnFinish += () =>
        {
            UnlockFocus();
        };
    }

    private readonly CountdownTimer FocusLockTimer = new();

    public bool IsFocusLocked { get; set; } = false;

    public bool IsFocusLockHold
    {
        get
        {
            return FocusLockTimer.IsEnabled;
        }
    }

    public int FocusLockHoldDuration
    {
        get
        {
            return Settings.GetFocusLockHoldDuration();
        }
        set
        {
            Settings.SetFocusLockHoldDuration(value);

            // 양방향 바인딩되는 속성으로, UI에 의해 변경시 여기에서 NotifyPropertyChanged를 트리거해요.
            NotifyPropertyChanged(nameof(FocusLockHoldDuration));
            NotifyPropertyChanged(nameof(StartFocusLockItemLabel));
        }
    }

    public Visibility IsWarningBorderVisible
    {
        get
        {
            return !IsFocusLocked || IsAnyAppActive ? Visibility.Hidden : Visibility.Visible;
        }
    }

    public DrawingImage? LockImage
    {
        get
        {
            string resourceName = IsFocusLocked ? "ic_lock" : "ic_lock_open_outline";

            return Application.Current.FindResource(resourceName) as DrawingImage;
        }
    }

    private readonly ToolTip _LockButtonToolTip = new();
    public ToolTip? LockButtonToolTip
    {
        get
        {
            _LockButtonToolTip.Content = $"{(int)Math.Ceiling(FocusLockTimer.TimeLeft.TotalMinutes)}분 남았습니다.";

            return IsFocusLockHold ? _LockButtonToolTip : null;
        }
    }

    public string StartFocusLockItemLabel
    {
        get
        {
            return $"{FocusLockHoldDuration}분간 강제 잠금";
        }
    }

    public void StartFocusLockWithHold()
    {
        LockFocusWithHold();

        Render();
    }
    private void LockFocusWithHold()
    {
        FocusLockTimer.Duration = TimeSpan.FromMinutes(FocusLockHoldDuration);
        FocusLockTimer.Start();

        IsFocusLocked = true;
        StartAnimation("LockingAnimation");
    }

    public void ToggleFocusLock()
    {
        if (IsFocusLocked)
        {
            UnlockFocus();
        }
        else
        {
            LockFocus();
        }

        Render();
    }

    private void LockFocus()
    {
        FocusLockTimer.Stop();
        IsFocusLocked = true;

        StartAnimation("LockingAnimation");
    }

    private void UnlockFocus()
    {
        if (IsFocusLockHold)
        {
            _LockButtonToolTip.IsOpen = true;
            Task.Delay(700).ContinueWith(_ => Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                _LockButtonToolTip.IsOpen = false;
            })));

            StartAnimation("ShakeHorizontalAnimation");

            return;
        }

        IsFocusLocked = false;
        StartAnimation("UnlockingAnimation");
    }

    private void RestoreFocusIfNeeded(IntPtr prev, IntPtr current)
    {
        if (!IsFocusLocked)
        {
            // 포커스 잠금이 없으면 아무 것도 하지 않습니다.
            return;
        }

        if (IsAnyAppActive)
        {
            // 등록된 앱 중 활성화된 앱이 있으면 정상적인 상태입니다.
            return;
        }

        if (WindowWatcher.SkipList.Contains(APIWrapper.GetForegroundWindowClass()))
        {
            // 현재 포커스가 시스템 UI로 가 있다면 넘어갑니다.
            return;
        }

        if (APIWrapper.IsThisProcessForeground())
        {
            // 현재 포커스가 이 프로세스라면 넘어갑니다.
            return;
        }

        // 위 아무 조건에도 해당하지 않았다면
        // 포커스를 빼앗아야 할 때입니다.
        // 포커스를 이전 앱에 주고 현재 앱을 줄입니다.
        Task.Delay(200).ContinueWith(_ =>
        {
            APIWrapper.MinimizeWindow(current);
            APIWrapper.SetForegroundWindow(prev);
        });
    }

    #endregion
    
    #region 타이머의 리셋

    public void ResetTimer()
    {
        InitGlobalTimer();

        Usage = null;
        TicksStartOffset = 0;
        TicksElapsedOffset = 0;
        ActiveTicksStartOffset = 0;
        ActiveTicksElapsedOffset = 0;

        RestoreAppSlots();

        TickAll();
        RenderAll();
    }

    #endregion
}