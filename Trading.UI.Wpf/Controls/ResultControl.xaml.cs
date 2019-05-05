using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Arts.Financial;
using Arts.WCharting;
using Trading.DataStructures.Enums;
using Trading.UI.Wpf.Utils;
using Trading.UI.Wpf.ViewModels;
using Trading.UI.Wpf.ViewModels.EventArgs;

namespace Trading.UI.Wpf.Controls
{
    /// <summary>
    /// Interaktionslogik für ResultControl.xaml
    /// </summary>
    public partial class ResultControl : UserControl
    {
        private TradingViewModel _model;

        public ResultControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (!(DataContext is TradingViewModel model))
                return;

            _model = model;
            //Register for events
            _model.BacktestCompletedEvent -= OnBacktestCompleted;
            _model.IndexBacktestCompletedEvent -= OnIndexBacktestCompleted;
            _model.BacktestCompletedEvent += OnBacktestCompleted;
            _model.IndexBacktestCompletedEvent += OnIndexBacktestCompleted;
        }

        private void OnIndexBacktestCompleted(object sender, IndexBacktestResultEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OnBacktestCompleted(object sender, BacktestResultEventArgs args)
        {
            BarChartControl.Data.Clear();

            var from = new SDate(args.PortfolioValuations[0].PortfolioAsof);
            var to = new SDate(args.PortfolioValuations.Last().PortfolioAsof);


            //Fints aus dem PortfolioValue erstellen
            var navFints = FINTS.Create(args.PortfolioValuations.Select(x => new Quote<double>(new SDate(x.PortfolioAsof), (double)x.PortfolioValue)));

            //zu ChartControl hinzufügen zuerst die yearly returns
            BarChartControl.Data.Add(new WLineChartBars(from.ToDateTime() - to.ToDateTime() > TimeSpan.FromDays(650)
                ? GetFints(navFints, from, to, false)
                : GetFints(navFints, from, to))
            {
                FillColor = Colors.AliceBlue,
                Color = Colors.LightBlue,
                Caption = "Backtest",
                Scale = WLineChartScale.Secondary
            });

            //ChartControl.Data.Add(new WLineChartFINTS(navFints) { Color = Colors.Blue, Caption = "Backtest", StrokeThickness = 0.75 });
            var indexFints = FINTS<double>.Empty;

            switch (args.Settings.IndexType)
            {
                case IndexType.Dax:
                    indexFints = FINTS.Create(FintsSecuritiesRepo.Dax.Value.Where(x => x.Date >= navFints.BeginDate && x.Date <= navFints.EndDate));
                    break;
                case IndexType.EuroStoxx50:
                    indexFints = FINTS.Create(FintsSecuritiesRepo.Eurostoxx50.Value.Where(x => x.Date >= navFints.BeginDate && x.Date <= navFints.EndDate));
                    break;
                case IndexType.MsciWorldEur:
                    indexFints = FINTS.Create(FintsSecuritiesRepo.MsciWorldEur.Value.Where(x => x.Date >= navFints.BeginDate && x.Date <= navFints.EndDate));
                    break;
                case IndexType.SandP500:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
            indexFints.DataType = FINTSDataType.Return;

            BarChartControl.Data.Add(new WLineChartFINTS(indexFints)
            {
                FillColor = Colors.LightCoral,
                Color = Colors.LightCoral,
                Caption = args.Settings.IndexType.ToString(),
                StrokeThickness = 0.75

            });
            BarChartControl.ViewBeginDate = navFints.BeginDate;
            BarChartControl.ViewEndDate = navFints.EndDate;
        }

        private static FINTS<double> GetFints(FINTS<double> navFints, SDate @from, SDate to, bool isMonthly = true)
        {
            return isMonthly
                ? navFints.CalcMonthReturnsFINTS(@from, to, Currency.GetCurrency("EUR"), false)
                : navFints.CalcYearReturnsFINTS(@from, to, Currency.GetCurrency("EUR"), false);
        }
    }
}
