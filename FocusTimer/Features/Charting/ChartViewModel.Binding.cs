﻿// ChartViewModel.Binding.cs
// 이 파일은 FocusTimer의 일부입니다.
// 
// © 2023 Potados <song@potados.com>
// 
// FocusTimer은(는) 자유 소프트웨어입니다.
// GNU General Public License v3.0을 준수하는 범위 내에서
// 자유롭게 변경, 수정하거나 배포할 수 있습니다.
// 
// 이 프로그램을 공유함으로서 다른 누군가에게 도움이 되기를 바랍니다.
// 다만 프로그램 배포와 함께 아무 것도 보증하지 않습니다. 자세한 내용은
// GNU General Public License를 참고하세요.
// 
// 라이센스 전문은 이 프로그램과 함께 제공되었을 것입니다. 만약 아니라면,
// 다음 링크에서 받아볼 수 있습니다: <https://www.gnu.org/licenses/gpl-3.0.txt>

using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FocusTimer.Features.Charting.Metric;
using FocusTimer.Features.Charting.Usages;

namespace FocusTimer.Features.Charting;

public partial class ChartViewModel
{
    public ObservableCollection<ISeries> SeriesCollection1 { get; set; }
    public ObservableCollection<ISeries> SeriesCollection2 { get; set; }
    public Axis[] SharedXAxis { get; set; }

    public Axis[] YAxis { get; set; }

    public IPaint<SkiaSharpDrawingContext> TooltipPaint { get; set; } = new SolidColorPaint(new SKColor(28, 49, 58))
    {
        FontFamily = "맑은 고딕"
    };

    public string SelectedDateString => _selectedDate == DateTime.MinValue ? "지난 21일" : _selectedDate.ToString("yyyy. MM. dd");

    public IEnumerable<PrimaryMetricItem> PrimaryMetrics => _processingService.GetPrimaryMetrics(_selectedDate);

    public IEnumerable<AppUsageItem> SelectedDateUsages => _processingService.GetAppUsagesAtDate(_selectedDate);
}