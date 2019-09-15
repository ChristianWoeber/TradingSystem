using System.Collections.Generic;
using Trading.Core.Backtest;

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