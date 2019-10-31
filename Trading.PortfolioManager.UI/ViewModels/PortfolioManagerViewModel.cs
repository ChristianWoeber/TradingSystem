using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Common.Lib.Data.Attributes;
using Common.Lib.UI.WPF.Core.Input;
using Trading.Core.Portfolio;
using Trading.DataStructures.Interfaces;
using Trading.PortfolioManager.UI.Wpf.Annotations;

namespace Trading.PortfolioManager.UI.Wpf.ViewModels
{
    public abstract class NotifcationBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PortfolioManagerViewModel : NotifcationBase
    {
        private readonly IPortfolioManager _portfolioManager;
        private ObservableCollection<TransactionViewModel> _currentTransactions;

        public PortfolioManagerViewModel(IPortfolioManager portfolioManager)
        {
            _portfolioManager = portfolioManager;

            //Commands
            //AllocatePortfolioCommand = new RelayCommand(() => Allocate())
        }

        public ObservableCollection<TransactionViewModel> CurrentTransactions =>
           _currentTransactions ?? (_currentTransactions = new ObservableCollection<TransactionViewModel>(_portfolioManager.CurrentPortfolio.Select(tr => new TransactionViewModel(tr))));

    }

    public class TransactionViewModel : NotifcationBase
    {
        private readonly ITransaction _transaction;

        public TransactionViewModel(ITransaction transaction)
        {
            _transaction = transaction;
        }

        [SmartDataGridColumnProperty("Name", true, ColumnSortIndex = 0)]
        public string Name { get; }

        [SmartDataGridColumnProperty("Ziel Amount EUR", true, ColumnSortIndex = 2)]
        public decimal TargetAmountEur
        {
            get => _transaction.TargetAmountEur;
            set
            {
                if (value == _transaction.TargetAmountEur)
                    return;
                _transaction.TargetAmountEur = value;
                OnPropertyChanged();
            }
        }

        [SmartDataGridColumnProperty("Stücke", true, ColumnSortIndex = 1)]
        public int Shares
        {
            get => _transaction.Shares;
            set
            {
                if (value == _transaction.Shares)
                    return;
                _transaction.Shares = value;
                OnPropertyChanged();
            }
        }
    }
}
