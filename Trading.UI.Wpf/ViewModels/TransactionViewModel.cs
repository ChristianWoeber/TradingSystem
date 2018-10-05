using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HelperLibrary.Database.Models;
using JetBrains.Annotations;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.ViewModels
{
    public class TransactionViewModel : INotifyPropertyChanged
    {
        #region Private Members

        private readonly ITransaction _transaction;
        private bool _isNew;

        #endregion

        #region Constructor

        public TransactionViewModel(ITransaction transaction, IScoringResult scoringResult)
        {
            _transaction = transaction;
            ScoringResult = scoringResult;
        }


        #endregion

        #region OneWay Properties

        public IScoringResult ScoringResult { get; }
        public int SecurityId => _transaction.SecurityId;
        public TransactionType Type => (TransactionType)_transaction.TransactionType;
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
    
        public decimal? Score => ScoringResult?.Score;

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