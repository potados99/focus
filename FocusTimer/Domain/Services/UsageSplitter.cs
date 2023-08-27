﻿using System;
using System.Collections.Generic;
using System.Linq;
using FocusTimer.Domain.Entities;

namespace FocusTimer.Domain.Services;

public class UsageSplitter<T> where T : IElapsable, new()
{
    private readonly T _usage;

    public UsageSplitter(T usage)
    {
        _usage = usage;
    }

    public IEnumerable<T> Split()
    {
        var result = new List<T>();

        var currentDate = _usage.StartedAt.Date;

        while (currentDate <= _usage.UpdatedAt.Date)
        {
            var isFirstDay = currentDate == _usage.StartedAt.Date;
            var isLastDay = currentDate == _usage.UpdatedAt.Date;

            if (isFirstDay && isLastDay)
            {
                result.Add(BuildUsage(_usage.StartedAt, _usage.UpdatedAt));
            }
            else if (isFirstDay)
            {
                result.Add(BuildUsage(_usage.StartedAt, currentDate.AddHours(24)));
            }
            else if (isLastDay)
            {
                result.Add(BuildUsage(currentDate, _usage.UpdatedAt));
            }
            else
            {
                result.Add(BuildUsage(currentDate, currentDate.AddHours(24)));
            }

            currentDate = currentDate.AddHours(24);
        }

        return result;
    }

    private T BuildUsage(DateTime startedAt, DateTime updatedAt)
    {
        return new T
        {
            StartedAt = startedAt,
            UpdatedAt = updatedAt,
            ElapsedTicks = (updatedAt - startedAt).Ticks
        };
    }

    public static IEnumerable<TRunningUsage>
        GetRunningUsagesSplitByDate<TUsage, TRunningUsage, TActiveUsage>(TUsage usage)
        where TUsage : IUsage<TRunningUsage>
        where TRunningUsage : class, IRunningUsage<TUsage, TActiveUsage>, new()
        where TActiveUsage : class, IActiveUsage<TRunningUsage>, new()
    {
        var runningUsages = usage.RunningUsages
            .SelectMany(u => new UsageSplitter<TRunningUsage>(u).Split())
            .ToList(); // 이렇게 이 시점에 컬렉션을 확보해야 아래에서 수정할 수 있습니다.

        var activeUsages = usage.RunningUsages
            .SelectMany(u => u.ActiveUsages)
            .SelectMany(u => new UsageSplitter<TActiveUsage>(u).Split())
            .ToList(); // 여기도 마찬가지!

        foreach (var runningUsage in runningUsages)
        {
            runningUsage.ParentUsage = usage;
            
            foreach (var activeUsage in activeUsages.Where(u => u.StartedAt.Date == runningUsage.StartedAt.Date))
            {
                activeUsage.ParentRunningUsage = runningUsage;

                runningUsage.ActiveUsages.Add(activeUsage);
            }
        }

        return runningUsages;
    }
}