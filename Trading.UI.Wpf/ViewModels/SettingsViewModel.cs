using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.ViewModels
{
    public class SettingsViewModel : IPortfolioSettings, INotifyPropertyChanged
    {

        public SettingsViewModel(IPortfolioSettings defaultSettings)
        {
            PortfolioSettings = defaultSettings;
        }

        public IPortfolioSettings PortfolioSettings { get; }

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

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        #endregion
    }
}
