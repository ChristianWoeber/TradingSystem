using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Common.Lib.UI.WPF.Core.Controls.Core;
using JetBrains.Annotations;
using Microsoft.Win32;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.ViewModels
{
    public class SettingsViewModel : IPortfolioSettings, INotifyPropertyChanged
    {
        private TradingDay _selectedTradingDay;
        private TradingIntervalUtil _selectedTradingInterval;

        public SettingsViewModel(IPortfolioSettings defaultSettings)
        {
            PortfolioSettings = defaultSettings;
            SelectedTradingDay = new TradingDay(PortfolioSettings.TradingDay);
            SelectedInterval = new TradingIntervalUtil(PortfolioSettings.Interval);

            //Commands
            //  OpenSaveDialogCommand = new RelayCommand(OnOpenSaveFileDialog);
            var baseDir = Path.GetFullPath(Path.Combine(Globals.BasePath, @"..\"));
            var initialDir = Path.Combine(baseDir, @"Backtests");
            if (!Directory.Exists(initialDir))
                Directory.CreateDirectory(initialDir);

            LoggingPath = initialDir;
            IndicesDirectory = Globals.IndicesBasePath;
        }


        public ICommand OpenSaveDialogCommand { get; set; }

        internal IPortfolioSettings PortfolioSettings { get; }

        private void OnOpenSaveFileDialog()
        {
            var baseDir = Path.GetFullPath(Path.Combine(Globals.BasePath, @"..\"));
            var initialDir = Path.Combine(baseDir, @"Backtests");
            if (!Directory.Exists(initialDir))
                Directory.CreateDirectory(initialDir);

            var dlg = new SaveFileDialog
            {
                CheckPathExists = true,
                InitialDirectory = initialDir,
                DefaultExt = ".zip",
                AddExtension = true,
                //Filter = "Files |.zip;"
            };

            if (dlg.ShowDialog() == true)
            {
                LoggingPath = dlg.FileName;
            }
        }


        public decimal MaximumInitialPositionSize
        {
            get => PortfolioSettings.MaximumInitialPositionSize;
            set
            {
                if (value == PortfolioSettings.MaximumInitialPositionSize)
                    return;
                PortfolioSettings.MaximumInitialPositionSize = value;
                OnPropertyChanged();
            }
        }

        public decimal MaximumPositionSize
        {
            get => PortfolioSettings.MaximumPositionSize;
            set
            {
                if (value == PortfolioSettings.MaximumPositionSize)
                    return;
                PortfolioSettings.MaximumPositionSize = value;
                OnPropertyChanged();
            }
        }

        public decimal CashPufferSize
        {
            get => PortfolioSettings.CashPufferSize;
            set
            {
                if (value == PortfolioSettings.CashPufferSize)
                    return;
                PortfolioSettings.CashPufferSize = value;
                OnPropertyChanged();
            }
        }

        public DayOfWeek TradingDay
        {
            get => PortfolioSettings.TradingDay;
            set
            {
                if (value == PortfolioSettings.TradingDay)
                    return;
                PortfolioSettings.TradingDay = value;
                OnPropertyChanged();
            }
        }

        public TradingDay SelectedTradingDay
        {
            get => _selectedTradingDay;
            set
            {
                _selectedTradingDay = value;
                OnPropertyChanged();
                TradingDay = value.Day;
            }
        }


        public TradingInterval Interval
        {
            get => PortfolioSettings.Interval;
            set
            {
                if (value == PortfolioSettings.Interval)
                    return;
                PortfolioSettings.Interval = value;
                OnPropertyChanged();
            }
        }

        public TradingIntervalUtil SelectedInterval
        {
            get => _selectedTradingInterval;
            set
            {
                if (value == _selectedTradingInterval)
                    return;
                _selectedTradingInterval = value;
                OnPropertyChanged();
                Interval = value.Interval;
            }
        }


        public decimal MaxTotaInvestmentLevel => PortfolioSettings.MaxTotaInvestmentLevel;

        public decimal InitialCashValue
        {
            get => PortfolioSettings.InitialCashValue;
            set
            {
                if (value == PortfolioSettings.InitialCashValue)
                    return;
                PortfolioSettings.InitialCashValue = value;
                OnPropertyChanged();
            }
        }

        public int MinimumHoldingPeriodeInDays
        {
            get => PortfolioSettings.MinimumHoldingPeriodeInDays;
            set
            {
                if (value == PortfolioSettings.MinimumHoldingPeriodeInDays)
                    return;
                PortfolioSettings.MinimumHoldingPeriodeInDays = value;
                OnPropertyChanged();
            }
        }

        public decimal ReplaceBufferPct
        {
            get => PortfolioSettings.ReplaceBufferPct;
            set
            {
                if (value == PortfolioSettings.ReplaceBufferPct)
                    return;
                PortfolioSettings.ReplaceBufferPct = value;
                OnPropertyChanged();
            }
        }

        public decimal MaximumPositionSizeBuffer
        {
            get => PortfolioSettings.MaximumPositionSizeBuffer;
            set
            {
                if (value == PortfolioSettings.MaximumPositionSizeBuffer)
                    return;
                PortfolioSettings.MaximumPositionSizeBuffer = value;
                OnPropertyChanged();
            }
        }

        public string LoggingPath
        {
            get => PortfolioSettings.LoggingPath;
            set
            {
                if (value == PortfolioSettings.LoggingPath)
                    return;
                PortfolioSettings.LoggingPath = value;
                OnPropertyChanged();
            }
        }

        public string IndicesDirectory
        {
            get => PortfolioSettings.IndicesDirectory;
            set
            {
                if (value == PortfolioSettings.IndicesDirectory)
                    return;
                PortfolioSettings.IndicesDirectory = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<TradingDay> AvailableTradingDays
        {
            get
            {
                yield return new TradingDay(DayOfWeek.Monday);
                yield return new TradingDay(DayOfWeek.Thursday);
                yield return new TradingDay(DayOfWeek.Wednesday);
                yield return new TradingDay(DayOfWeek.Thursday);
                yield return new TradingDay(DayOfWeek.Friday);
            }
        }

        public IEnumerable<TradingIntervalUtil> AvailableTradingIntervals
        {
            get
            {
                yield return new TradingIntervalUtil(TradingInterval.Monthly);
                yield return new TradingIntervalUtil(TradingInterval.ThreeWeeks);
                yield return new TradingIntervalUtil(TradingInterval.TwoWeeks);
                yield return new TradingIntervalUtil(TradingInterval.Weekly);
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion
    }

    public class TradingIntervalUtil : IFilterableProperty
    {
        public readonly TradingInterval Interval;

        public TradingIntervalUtil(TradingInterval interval)
        {
            Interval = interval;
        }

        public string FilterableText => Interval.ToString();
        public object Model => this;
    }

    public class TradingDay : IFilterableProperty
    {
        public TradingDay(DayOfWeek day)
        {
            Day = day;
        }

        public DayOfWeek Day { get; }

        public string FilterableText => Day.ToString();
        public object Model => this;
    }
}
