using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Trading.PortfolioManager.Exposure;
using Trading.DataStructures.Enums;

namespace Trading.UI.Wpf.ViewModels
{
    public interface IIndexBacktestSettings
    {
        IndexType TypeOfIndex { get; set; }
    }
}