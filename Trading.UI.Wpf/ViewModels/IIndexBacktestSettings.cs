using HelperLibrary.Trading.PortfolioManager;

namespace Trading.UI.Wpf.ViewModels
{
    public interface IIndexBacktestSettings
    {
        IndexType TypeOfIndex { get; set; }
    }
}