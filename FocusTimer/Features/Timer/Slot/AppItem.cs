﻿using System;
using FocusTimer.Domain.Entities;
using FocusTimer.Domain.Services;
using FocusTimer.Lib;
using FocusTimer.Lib.Utility;

namespace FocusTimer.Features.Timer.Slot;

/// <summary>
/// 타이머 슬롯에 등록되는 앱을 나타냅니다.
/// 타이머 슬롯에는 앱이 등록되어 있을 수도, 그렇지 않을 수도 있습니다.
/// </summary>
public partial class AppItem : StopwatchRunner, IDisposable
{
    private readonly AppService _appService = Modules.Get<AppService>();
    private readonly AppUsageService _appUsageService = Modules.Get<AppUsageService>();
    private readonly UserActivityMonitor _activityMonitor = Modules.Get<UserActivityMonitor>();
    private readonly EventService _eventService = Modules.Get<EventService>();

    public AppItem(IntPtr windowHandle) : this(APIWrapper.GetProcessByWindowHandle(windowHandle)?.ExecutablePath())
    {
    }
    
    public AppItem(Domain.Entities.App app) : this(app.ExecutablePath)
    {
    }

    private AppItem(string? executablePath)
    {
        if (string.IsNullOrEmpty(executablePath))
        {
            throw new Exception("TimerApp의 생성자에 executablePath가 null로 들어왔습니다. 이래서는 안 됩니다!");
        }

        App = _appService.GetOrCreateApp(executablePath);

        RegisterEvents();
        LoadUsage();
    }
    
    public void Dispose()
    {
        this.GetLogger().Info("이 AppItem을 정리합니다.");
        UnregisterEvents();
    }

    public Domain.Entities.App App { get; }

    private AppUsage? _usage;

    private void RegisterEvents()
    {
        _eventService.OnTick += OnTick;
        _eventService.OnActivated += OnActivated;
        _eventService.OnFocusChanged += OnFocusChanged;
    }

    private void UnregisterEvents()
    {
        _eventService.OnTick -= OnTick;
        _eventService.OnActivated -= OnActivated;
        _eventService.OnFocusChanged -= OnFocusChanged;
    }

    private void OnTick()
    {
        StartOrStopActiveTimer(IsActive);
        UpdateUsage();
    }

    private void OnActivated()
    {
        if (IsActive)
        {
            this.GetLogger().Info("Activated 이벤트로 인해 새로운 AppActiveUsage를 생성합니다.");

            _usage?.OpenNewActiveUsage();
        }
    }
    
    private void OnFocusChanged(IntPtr prev, IntPtr current)
    {
        if (IsActive)
        {
            this.GetLogger().Info("Focused 이벤트로 인해 새로운 AppActiveUsage를 생성합니다.");

            _usage?.OpenNewActiveUsage();
        }
    }

    private void LoadUsage()
    {
        this.GetLogger().Debug("AppUsage를 불러옵니다.");
        
        _usage = _appUsageService.GetLastUsage(App) ?? _appUsageService.CreateNewUsage(App);

        IsCountedOnConcentrationCalculation = _usage.IsConcentrated;

        Restart();
        AddOffset(_usage.ActiveElapsed, TimeSpan.Zero);
    }

    private void UpdateUsage()
    {
        if (_usage == null)
        {
            return;
        }
        
        _usage.TouchUsage(IsCountedOnConcentrationCalculation);
        
        if (IsActive)
        {
            _usage.TouchActiveUsage();
        }

        _appUsageService.SaveRepository();
    }
}