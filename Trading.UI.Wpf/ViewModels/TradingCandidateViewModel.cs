using System;
using Common.Lib.Data.Attributes;
using Common.Lib.Data.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.ViewModels
{
    public class TradingCandidateViewModel
    {
        private readonly ITradingCandidateBase _tradingCandidateBase;
        private readonly int _index;

        public TradingCandidateViewModel(ITradingCandidateBase tradingCandidateBase, int index)
        {
            _tradingCandidateBase = tradingCandidateBase;
            _index = index;
        }

        #region Columns

        [SmartDataGridColumnProperty("Name", true, ColumnSortIndex = 0, IsFilterable = true)]
        public string Name => _tradingCandidateBase.Record.Name;

        [SmartDataGridColumnProperty("Id", true, ColumnSortIndex = 1, IsFilterable = true)]
        public int SecurityId => _tradingCandidateBase.Record.SecurityId;

        [SmartDataGridColumnProperty("Datum", true, ColumnSortIndex = 2)]
        public DateTime Asof => _tradingCandidateBase.Record.Asof;

        [SmartDataGridColumnProperty("Score", true, ColumnSortIndex = 3, SortDirection = SmartDataGridSortDirection.Descending)]
        public decimal Score => _tradingCandidateBase.ScoringResult.Score;

        [SmartDataGridColumnProperty("Perf 10", true, ColumnSortIndex = 4, StringFormat = "P2")]
        public decimal Performance10 => _tradingCandidateBase.ScoringResult.Performance10;

        [SmartDataGridColumnProperty("Perf 30", true, ColumnSortIndex = 5, StringFormat = "P2")]
        public decimal Performance30 => _tradingCandidateBase.ScoringResult.Performance30;

        [SmartDataGridColumnProperty("Perf 90", true, ColumnSortIndex = 6, StringFormat = "P2")]
        public decimal Performance90 => _tradingCandidateBase.ScoringResult.Performance90;

        [SmartDataGridColumnProperty("Perf 250", true, ColumnSortIndex = 7, StringFormat = "P2")]
        public decimal Performance250 => _tradingCandidateBase.ScoringResult.Performance250;

        [SmartDataGridColumnProperty("Vola", true, ColumnSortIndex = 8, StringFormat = "P2")]
        public decimal? Vola => _tradingCandidateBase.ScoringResult.Volatility;

        [SmartDataGridColumnProperty("Valid", true, ColumnSortIndex = 9, ColumnType = SmartDataGridColumnType.Image)]
        public bool Valid => _tradingCandidateBase.ScoringResult.IsValid;

        [SmartDataGridColumnProperty("Rank", true, ColumnSortIndex = 10)]
        public int Rank => _index + 1;

        #endregion
    }
}