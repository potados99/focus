﻿using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTimer.Lib.Component;

public abstract class BaseWindow<T> : Window where T : BaseViewModel
{
    protected readonly T ViewModel = App.Provider.GetService<T>()!;

    protected BaseWindow()
    {
        DataContext = ViewModel;

        Loaded += (_, _) =>
        {
            OnLoaded();
            ViewModel.OnLoaded();
        };
        
        OnInitialize();
        ViewModel.OnInitialize();
    }

    protected abstract void OnInitialize();

    protected virtual void OnLoaded() {}
}