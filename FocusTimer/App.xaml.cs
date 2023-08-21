﻿using System;
using System.Windows;
using FocusTimer.Domain.Services;
using FocusTimer.Features.Charting;
using FocusTimer.Features.License;
using FocusTimer.Features.Timer;
using FocusTimer.Lib;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTimer;

public partial class App : Application
{
    public static readonly ServiceProvider Provider =
        ConfigureServices(new ServiceCollection()).BuildServiceProvider();

    private static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // ViewModels
        services
            .AddTransient<MainViewModel>()
            .AddTransient<ChartViewModel>()
            .AddTransient<LicenseViewModel>();

        // Services
        services
            .AddSingleton<LicenseService>();

        // Others
        services
            .AddSingleton<UserActivityMonitor>();

        return services;
    }

    [STAThread]
    public static void Main()
    {
        var application = new App();
        application.InitializeComponent();
        application.Run();
    }
}