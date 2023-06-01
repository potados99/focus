﻿using FocusTimer.Lib;
using FocusTimer.Lib.Component;
using FocusTimer.Lib.Models;
using FocusTimer.Lib.Utility;
using FocusTimer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace FocusTimer
{
    internal class MainViewModel : BaseModel
    {
        public MainViewModel()
        {
            foreach (var slot in TimerSlots)
            {
                slot.OnRegisterApplication += () => StartRegisteringApplication(slot);
                slot.OnClearApplication += () => ClearApplication(slot);
            }

            Watcher watcher = new();

            watcher.OnFocused += (prev, current) =>
            {
                if (CurrentRegisteringTimerSlot != null)
                {
                    FinishRegisteringApp(new TimerApp(current));
                }

                RestoreFocusIfNeeded(prev, current);

                RenderAll();
            };

            watcher.StartListening();
        }

        public void Loaded()
        {
            RestoreApps();
            StartTimer();
            RenderAll();
        }

        public void RenderAll()
        {
            Render();

            foreach (var slot in TimerSlots)
            {
                slot.Render();
            }
        }

        public void Render()
        {
            if (IsAnyAppActive)
            {
                ActiveStopwatch.Start();
            }
            else
            {
                ActiveStopwatch.Stop();
            }

            NotifyPropertyChanged(nameof(ElapsedTime));
            NotifyPropertyChanged(nameof(IsAnyAppActive));
            NotifyPropertyChanged(nameof(IsWarningBorderVisible));

            NotifyPropertyChanged(nameof(IsFocusLocked));
            NotifyPropertyChanged(nameof(IsFocusLockHold));
            NotifyPropertyChanged(nameof(LockImage));
            NotifyPropertyChanged(nameof(LockButtonToolTip));

            NotifyPropertyChanged(nameof(Concentration));
        }

        #region Main Window의 확장 및 축소

        private bool expanded = true;
        public bool Expanded
        {
            get
            {
                return expanded;
            }
            set
            {
                expanded = value;
                NotifyPropertyChanged(nameof(Expanded));
                NotifyPropertyChanged(nameof(ExpandablePartLength)); // 얘가 WindowHeight보다 먼저 바뀌어야 탈이 없습니다.
                NotifyPropertyChanged(nameof(WindowHeight));
            }
        }

        public void ToggleExpanded()
        {
            if (TimerSlots.Any(s => s.IsWaitingForApp))
            {
                return;
            }

            Expanded = !Expanded;
        }

        private readonly int windowHeight = 160;
        public int WindowHeight
        {
            get
            {
                if (Expanded)
                {
                    return windowHeight;
                }
                else
                {
                    int borderThickness = 1;
                    int separatorThickness = 1;
                    double contentGridRowLengthStar = fixedPartLength.Value;
                    double expandableGridRowLengthStar = expadedLength.Value;
                    double expandableGridRowLength = (double)windowHeight / (contentGridRowLengthStar + expandableGridRowLengthStar) * contentGridRowLengthStar;
                    return (int)expandableGridRowLength + borderThickness + separatorThickness;
                }
            }
            set
            {
                // 왜인지는 모르겠는데 이 WindowHeight은 양방향 바인딩으로 넣어 주어야 잘 돌아갑니다...
            }
        }

        private GridLength fixedPartLength = new GridLength(1.4, GridUnitType.Star);
        private GridLength expadedLength = new GridLength(4, GridUnitType.Star);
        private GridLength collapsedLength = new GridLength(0);

        public GridLength FixedPartLength
        {
            get
            {
                return fixedPartLength;
            }
            set
            {

            }
        }

        public GridLength ExpandablePartLength
        {
            get
            {
                return Expanded ? expadedLength : collapsedLength;
            }
            set
            {

            }
        }

        #endregion

        #region 스탑워치와 글로벌 틱 타이머

        private readonly Stopwatch ActiveStopwatch = new();
        private readonly Stopwatch AlwaysOnStopwatch = new();
        private readonly DispatcherTimer OneSecTickTimer = new();

        public string ElapsedTime
        {
            get
            {
                return ActiveStopwatch.ElapsedString();
            }
        }

        public void StartTimer()
        {
            OneSecTickTimer.Tick += (_, _) => RenderAll();
            OneSecTickTimer.Interval = new TimeSpan(0, 0, 0, 1);
            OneSecTickTimer.Start();

            AlwaysOnStopwatch.Start();
            ActiveStopwatch.Start();
        }

        #endregion

        #region 포커스 잠금과 홀드 타이머

        private readonly DispatcherTimer FocusLockTimer = new();
        private TimeSpan FocusLockTimeSpan = new();

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
                _LockButtonToolTip.Content = $"{(int)Math.Ceiling(FocusLockTimeSpan.TotalMinutes)}분 남았습니다.";

                return IsFocusLockHold ? _LockButtonToolTip : null;
            }
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
            FocusLockTimeSpan = new TimeSpan(0, FocusLockHoldDuration, 0);
            FocusLockTimer.Interval = TimeSpan.FromSeconds(1);
            FocusLockTimer.Tick += (_, _) =>
            {
                if (FocusLockTimeSpan == TimeSpan.Zero || FocusLockTimeSpan.TotalSeconds <= 0)
                {
                    FocusLockTimer.Stop();
                    UnlockFocus();
                }
                else
                {
                    FocusLockTimeSpan = FocusLockTimeSpan.Add(TimeSpan.FromSeconds(-1));
                }

                Render();
            };
            FocusLockTimer.Start();

            IsFocusLocked = true;
            StartAnimation("LockingAnimation");
        }

        private void UnlockFocus()
        {
            if (IsFocusLockHold)
            {
                _LockButtonToolTip.IsOpen = true;
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

            if (Watcher.SkipList.Contains(APIWrapper.GetForegroundWindowClass()))
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

        #region 타이머 슬롯의 등록, 초기화 및 상태

        public List<TimerSlotViewModel> TimerSlots { get; } = new List<TimerSlotViewModel>() {
            new TimerSlotViewModel() { SlotNumber = 0 },
            new TimerSlotViewModel() { SlotNumber = 1 },
            new TimerSlotViewModel() { SlotNumber = 2 },
        };

        private TimerSlotViewModel? CurrentRegisteringTimerSlot
        {
            get
            {
                return TimerSlots.FirstOrDefault(s => s.IsWaitingForApp);
            }
        }

        public bool IsAnyAppActive
        {
            get
            {
                return TimerSlots.Any(s => s.IsAppActive);
            }
        }

        private void StartRegisteringApplication(TimerSlotViewModel slot)
        {
            if (CurrentRegisteringTimerSlot != null)
            {
                return;
            }

            slot.StartWaitingForApp();

            Render();
        }

        private void FinishRegisteringApp(TimerApp app)
        {
            CurrentRegisteringTimerSlot?.StopWaitingAndRegisterApp(app);

            SaveApps();
            Render();

            NotifyPropertyChanged(nameof(ConcentrationContextMenu));
        }

        private void ClearApplication(TimerSlotViewModel slot)
        {
            if (CurrentRegisteringTimerSlot != null)
            {
                return;
            }

            slot.ClearRegisteredApp();

            SaveApps();
            Render();

            NotifyPropertyChanged(nameof(ConcentrationContextMenu));
        }

        #endregion

        #region 타이머 슬롯의 저장 및 복구

        public void SaveApps()
        {
            Settings.SetApps(TimerSlots.Select(s => s.GetAppExecutablePath()).ToList());
        }

        public void RestoreApps()
        {
            foreach (var (app, index) in Settings.GetApps().WithIndex())
            {
                TimerSlots[index].RestoreApp(app);
            }
        }

        #endregion

        #region 집중도

        public string Concentration
        {
            get
            {
                var elapsedTotal = TimerSlots
                    .Where(s => s.CurrentApp?.IsCountedOnConcentrationCalculation ?? false)
                    .Sum(s => s.CurrentApp?.ElapsedMilliseconds ?? 0);

                if (elapsedTotal == 0)
                {
                    return "0%";
                }

                double concentration = 100 * elapsedTotal / AlwaysOnStopwatch.ElapsedMilliseconds;

                return concentration + "%";
            }
        }

        public BindableMenuItem WhichAppToIncludeMenuItem = new()
        {
            IsCheckable = false,
            IsChecked = false,
            Icon = new Image() { Source = Application.Current.FindResource("ic_calculator_variant_outline") as DrawingImage },
            Header = "집중도 계산에 포함할 프로그램",
        };

        public BindableMenuItem[] ConcentrationContextMenu
        {
            get
            {
                WhichAppToIncludeMenuItem.Children = TimerSlots
                    .Where(s => s.CurrentApp != null)
                    .Select(s =>
                    {
                        var app = s.CurrentApp;
                        var item = new BindableMenuItem()
                        {
                            IsCheckable = true,
                            IsChecked = app.IsCountedOnConcentrationCalculation,
                            Header = app.AppName
                        };

                        item.OnCheck += (isChecked) =>
                        {
                            app.IsCountedOnConcentrationCalculation = isChecked;
                            NotifyPropertyChanged(nameof(ConcentrationContextMenu));
                        };

                        return item;
                    })
                    .ToArray();
                    
                return new BindableMenuItem[] { WhichAppToIncludeMenuItem };
            }
        }

        #endregion

        #region 기타 UI

        private void StartAnimation(string name)
        {
            Storyboard? sb = Application.Current.MainWindow.Resources[name] as Storyboard;
            sb?.Begin();
        }

        #endregion
    }
}
