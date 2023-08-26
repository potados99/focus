﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using FocusTimer.Lib.Utility;

namespace FocusTimer.Domain.Entities;

/// <summary>
/// 리셋과 리셋 사이의 앱 사용 정보를 나타내는 엔티티입니다.
/// 타이머가 리셋되면 새로운 엔티티가 생깁니다.
/// </summary>
public class AppUsage
{
    /// <summary>
    /// PK입니다.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 사용 현황을 기록하는 앱입니다.
    /// </summary>
    public App App { get; set; }

    /// <summary>
    /// 앱이 슬롯에 리셋 이후 처음으로 등록된 시각입니다.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// 이 엔티티가 업데이트된 마지막 시각입니다.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// 리셋 이후 앱이 등록되어 있는 동안 흐른 시간입니다(tick).
    /// 실제로 타이머가 켜져 있는 동안에만 증가하기 때문에,
    /// <see cref="RunningUsage"/>의 <see cref="ElapsedTicks"/>와 사실상 같습니다.
    /// </summary>
    public long ElapsedTicks { get; set; }
    
    /// <summary>
    /// 이 앱을 집중도 계산에 포함할지 여부를 나타냅니다.
    /// </summary>
    public bool IsConcentrated { get; set; }

    public ICollection<AppRunningUsage> RunningUsages { get; } = new List<AppRunningUsage>();
    
    [NotMapped] public AppRunningUsage RunningUsage => GetLastRunningUsage() ?? OpenNewRunningUsage();

    [NotMapped] public TimeSpan Elapsed => new(ElapsedTicks);
    [NotMapped] public TimeSpan RunningElapsed => new(RunningUsages.Sum(u => u.Elapsed.Ticks));
    [NotMapped] public TimeSpan ActiveElapsed => new(RunningUsages.Sum(u => u.ActiveElapsed.Ticks));

    public void TouchUsage(bool isConcentrated)
    {
        this.GetLogger().Debug("AppUsage를 갱신합니다.");

        UpdatedAt = DateTime.Now;
        ElapsedTicks += TimeSpan.TicksPerSecond;
        IsConcentrated = isConcentrated;
    }

    private AppRunningUsage? GetLastRunningUsage()
    {
        var usage = RunningUsages.LastOrDefault();
        if (usage != null)
        {
            this.GetLogger().Debug($"기존의 AppRunningUsage를 가져왔습니다: {usage}");
        }

        return usage;
    }
    
    public AppRunningUsage OpenNewRunningUsage()
    {
        this.GetLogger().Debug("새로운 AppRunningUsage를 생성합니다.");
        
        var usage = new AppRunningUsage
        {
            StartedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            AppUsage = this
        };
        
        RunningUsages.Add(usage);

        return usage;
    }

    public override string ToString()
    {
        return $"AppUsage(Id={Id}, Elapsed={Elapsed.ToSixDigits()}, ActiveElapsed={ActiveElapsed.ToSixDigits()})";
    }
}