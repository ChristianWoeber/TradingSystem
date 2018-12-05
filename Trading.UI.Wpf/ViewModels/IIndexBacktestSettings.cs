using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Trading.PortfolioManager.Exposure;

namespace Trading.UI.Wpf.ViewModels
{
    public interface IIndexBacktestSettings
    {
        IndexType TypeOfIndex { get; set; }
    }
}