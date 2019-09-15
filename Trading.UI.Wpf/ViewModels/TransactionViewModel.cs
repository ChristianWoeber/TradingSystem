using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HelperLibrary.Database.Models;
using JetBrains.Annotations;
using Trading.Core.Models;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.ViewModels
{
    public class TransactionViewModel : INotifyPropertyChanged
    {
        #region Private Members

        private readonly ITransaction _transaction;
        private bool _isNew;
        private bool _isStop;

        #endregion

        #region Constructor

        public TransactionViewModel(ITransaction transaction, IScoringResult scoringResult)
        {
            _transaction = transaction;
            ScoringResult = scoringResult;
        }

        public TransactionViewModel(ITransaction transaction, ScoringTraceModel scoringResult)
        {
            _transaction = transaction;
            ScoringTraceResult = scoringResult;
        }

        public TransactionViewModel(ITransaction transaction, IScoringResult scoringResult, bool isStop)
        {
            _transaction = transaction;
            ScoringResult = scoringResult;
            IsStop = isStop;
        }


        #endregion

        #region OneWay Properties

        public IScoringResult ScoringResult { get; }

        public ScoringTraceModel ScoringTraceResult { get; }

        public int SecurityId => _transaction.SecurityId;
        public TransactionType Type => _transaction.TransactionType;
        public decimal EffectiveAmountEur => _transaction.EffectiveAmountEur;
        public decimal TargetWeight => _transaction.TargetWeight;
        public DateTime TransactionDateTime => _transaction.TransactionDateTime;
        public int Shares => _transaction.Shares;

        #endregion

        #region TwoWay Properties

        /// <summary>
        /// Das Flag für die GUI
        /// </summary>
        public bool IsNew
        {
            get => _isNew;
            set
            {
                if (value == _isNew)
                    return;
                _isNew = value;
                OnPropertyChanged();
            }
        }

        public bool IsBuy => _transaction.Shares > 0;

        public bool IsStop
        {
            get => _isStop;
            set
            {
                if (value == _isStop)
                    return;
                _isStop = value;
                OnPropertyChanged();
            }
        }

        public decimal? Score => ScoringResult?.Score ?? ScoringTraceResult.PerformanceScore;

        #endregion

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