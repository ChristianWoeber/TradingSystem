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
    /// Interaktionslogik für MainChartControl.xaml
    /// </summary>
    public partial class MainChartControl : UserControl
    {
        private TradingViewModel _model;

        public MainChartControl()
        {
            InitializeComponent();

            //Events registrieren
            DataContextChanged += OnDataContextChanged;
            ChartControl.PreviewMouseLeftButtonDown += OnChartControlClicked;
            Unloaded += (sender, args) =>
            {
                //De-Register events
                _model.BacktestCompletedEvent -= OnBacktestCompleted;
                _model.IndexBacktestCompletedEvent -= OnIndexBacktestCompleted;
                _model.MoveCursorToNextTradingDayEvent -= OnMoveCursorToNextTradingDay;
                _model.MoveCursorToNextStoppDayEvent -= OnMoveCursorToNextStoppDayEvent;
            };

        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (!(DataContext is TradingViewModel model))
                return;

            if (_model != null)
                return;

            _model = model;

            //Register events
            _model.BacktestCompletedEvent += OnBacktestCompleted;
            _model.IndexBacktestCompletedEvent += OnIndexBacktestCompleted;
            _model.MoveCursorToNextTradingDayEvent += OnMoveCursorToNextTradingDay;
            _model.MoveCursorToNextStoppDayEvent += OnMoveCursorToNextStoppDayEvent;
        }

        private void OnMoveCursorToNextStoppDayEvent(object sender, EventArgs e)
        {
            if (ChartControl.Cursors[1]?.IsSet == false)
                return;

            var date = ChartControl.Cursors[1]?.CursorDate;
            if (date == null || date.Value <= DateTime.MinValue)
                return;

            _model.UpdateHoldings(date.Value.ToDateTime());
        }

        private void OnMoveCursorToNextTradingDay(object sender, MoveToTradingDayEventArgs args)
        {
            if (ChartControl.Cursors[1]?.IsSet == false)
                return;

            var date = ChartControl.Cursors[1]?.CursorDate;
            if (date == null || date.Value <= DateTime.MinValue)
                return;
            //der Modifier auf Basis der IsNext Property
            var modifier = args.IsNext ? 1 : -1;

            //Zum nächsten Trading Tag gehen
            var temp = date.Value.AddDays(modifier);

            while (temp.DayOfWeekEnum != args.TradingDay)
            {
                temp = temp.AddDays(modifier);
            }
            //und den Cursor entsprechend setzen
            ChartControl.Cursors[1].CursorDate = temp;
            _model.UpdateHoldings(temp, true);
        }




        private void OnIndexBacktestCompleted(object sender, IndexBacktestResultEventArgs e)
        {
            ChartControl.Data.Clear();

            var indexFints = FINTS.Create(e.Results.Select(x => new Quote<double>(new SDate(x.Asof), (double)x.IndexLevel)), "Index");
            var simulationFints = FINTS.Create(e.Results.Select(x => new Quote<double>(new SDate(x.Asof), (double)x.SimulationNav)), "Simulation");
            var allocationFints = FINTS.Create(e.Results.Select(x => new Quote<double>(new SDate(x.Asof), (double)x.MaximumAllocationToRisk)), "Aktienquote");
            allocationFints.DataType = FINTSDataType.Exposure;

            //zu ChartControl hinzufügen
            ChartControl.Data.Add(new WLineChartFINTS(allocationFints)
            {
                FillColor = Colors.AliceBlue,
                Color = Colors.LightBlue,
                FillMode = WLCFillMode.FillAlpha,
                FillAlpha = 0.25,
                StairSteps = true
            });
            ChartControl.Data.Add(new WLineChartFINTS(simulationFints) { Color = Colors.Blue, StrokeThickness = 0.75 });
            ChartControl.Data.Add(new WLineChartFINTS(indexFints) { Color = Colors.LightCoral, StrokeThickness = 0.75 });
            ChartControl.ViewBeginDate = simulationFints.BeginDate;
            ChartControl.ViewEndDate = simulationFints.EndDate;
            ChartControl.PrimaryScale.Logarithmic = true;

        }

        private void OnBacktestCompleted(object sender, BacktestResultEventArgs args)
        {
            ChartControl.Data.Clear();
            //Fints aus dem PortfolioValue erstellen
            var navFints = FINTS.Create(args.PortfolioValuations.Select(x => new Quote<double>(new SDate(x.PortfolioAsof), (double)x.PortfolioValue)));
            var allocationFints = FINTS.Create(args.PortfolioValuations.Select(x => new Quote<double>(new SDate(x.PortfolioAsof), Convert.ToDouble(x.AllocationToRisk))), "Investitionsgrad");
            allocationFints.DataType = FINTSDataType.Exposure;

            //zu ChartControl hinzufügen zuerst die Aktienquote
            ChartControl.Data.Add(new WLineChartFINTS(allocationFints)
            {
                FillColor = Colors.AliceBlue,
                Color = Colors.LightBlue,
                FillMode = WLCFillMode.FillAlpha,
                FillAlpha = 0.25
            });

            var indexFints = GetIndexFints(args, navFints);

            ChartControl.Data.Add(new WLineChartFINTS(navFints) { Color = Colors.Blue, Caption = "Backtest", StrokeThickness = 0.75 });
            ChartControl.Data.Add(new WLineChartFINTS(indexFints) { Color = Colors.LightCoral, Caption = args.Settings.IndexType.ToString(), StrokeThickness = 0.75 });
            ChartControl.ViewBeginDate = navFints.BeginDate;
            ChartControl.ViewEndDate = navFints.EndDate;
            ChartControl.PrimaryScale.Logarithmic = true;
        }

        private static FINTS<double> GetIndexFints(BacktestResultEventArgs args, FINTS<double> navFints)
        {
            var indexFints = FINTS<double>.Empty;

            switch (args.Settings.IndexType)
            {
                case IndexType.Dax:
                    indexFints =
                        FINTS.Create(FintsSecuritiesRepo.Dax.Value.Where(x =>
                            x.Date >= navFints.BeginDate && x.Date <= navFints.EndDate));
                    break;
                case IndexType.EuroStoxx50:
                    indexFints =
                        FINTS.Create(FintsSecuritiesRepo.Eurostoxx50.Value.Where(x =>
                            x.Date >= navFints.BeginDate && x.Date <= navFints.EndDate));
                    break;
                case IndexType.MsciWorldEur:
                    indexFints = FINTS.Create(FintsSecuritiesRepo.MsciWorldEur.Value.Where(x =>
                        x.Date >= navFints.BeginDate && x.Date <= navFints.EndDate));
                    break;
                case IndexType.SandP500:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }

            //indexFints.DataType = FINTSDataType.Return;
            return indexFints;
        }

        private void OnChartControlClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_model == null)
                return;

            if (!_model.HasPortfolioManager)
                return;

            if (ChartControl.Cursors[0]?.IsSet == false)
                return;

            var date = ChartControl.Cursors[0]?.CursorDate;
            if (date == null || date.Value <= DateTime.MinValue)
                return;
            _model.ChartDate = date.Value;
            _model.UpdateHoldings(date.Value.ToDateTime());
            _model.UpdateCash(date.Value.ToDateTime());
        }
    }
}
