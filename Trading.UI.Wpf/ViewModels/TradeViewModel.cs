using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Common.Lib.Data.Attributes;
using Common.Lib.Data.Enums;
using JetBrains.Annotations;

namespace Trading.UI.Wpf.ViewModels
{
    public class TradeViewModel : INotifyPropertyChanged
    {
        private readonly Trade _trade;

        public TradeViewModel(Trade trade)
        {
            _trade = trade;
            Name = TradingViewModel.NameCatalog[trade.Opening.SecurityId];
        }

        [SmartDataGridColumnProperty("SecurityId", true, ColumnSortIndex = 6)]
        public int? SecurityId => _trade.Opening?.SecurityId ?? -1;

        [SmartDataGridColumnProperty("Wertpapier", true, ColumnSortIndex = 0)]
        public string Name { get; }

        [SmartDataGridColumnProperty("OpenDate", true, StringFormat = "d", ColumnSortIndex = 1)]
        public DateTime? OpenDateTime => _trade.Opening?.TransactionDateTime;

        [SmartDataGridColumnProperty("Dip", true, StringFormat = "n", ColumnSortIndex = 2)]
        public int HoldingPeriode => _trade.HoldingPeriodeInDays;

        [SmartDataGridColumnProperty("TotalReturn", true, StringFormat = "p2", ColumnSortIndex = 3, SortDirection = SmartDataGridSortDirection.Descending)]
        public decimal TotalReturn => _trade.TotalReturn;

        [SmartDataGridColumnProperty("average Weight", true, StringFormat = "p2", ColumnSortIndex = 4)]
        public decimal AverageWeight => _trade.AveragePortfolioWeight;

        [SmartDataGridColumnProperty("IsNotValid", true, ColumnType = SmartDataGridColumnType.Image, ColumnSortIndex = 5)]
        public bool IsNotValid => !_trade.IsValid;



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