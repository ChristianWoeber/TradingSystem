using System.ComponentModel;
using System.Runtime.CompilerServices;
using Trading.Core.Portfolio;
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

        public PortfolioManagerViewModel(IPortfolioManager portfolioManager)
        {
            _portfolioManager = portfolioManager;
        }
    }
}
