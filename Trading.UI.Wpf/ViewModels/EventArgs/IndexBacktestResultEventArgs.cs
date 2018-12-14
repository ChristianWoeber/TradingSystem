using System.Collections.Generic;
using HelperLibrary.Trading.PortfolioManager;

namespace Trading.UI.Wpf.ViewModels.EventArgs
{
    public class IndexBacktestResultEventArgs
    {
        public List<IIndexBackTestResult> Results { get; }

        public IndexBacktestResultEventArgs(List<IIndexBackTestResult> results)
        {
            Results = results;
        }
    }
}