using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Arts.Financial;
using Arts.WCharting;
using Trading.UI.Wpf.Utils;
using Trading.UI.Wpf.ViewModels;

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
            ChartControl.PreviewMouseLeftButtonDown += OnChartControlClicked; ;

        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (!(DataContext is TradingViewModel model))
                return;

            _model = model;
            //Register events
            _model.BacktestCompletedEvent += OnBacktestCompleted;
            _model.IndexBacktestCompletedEvent += OnIndexBacktestCompleted;
            _model.MoveCursorToNextTradingDayEvent += OnMoveCursorToNextTradingDay;
        }


        private void OnMoveCursorToNextTradingDay(object sender, DayOfWeek tradingDay)
        {
            if (ChartControl.Cursors[1]?.IsSet == false)
                return;

            var date = ChartControl.Cursors[1]?.CursorDate;
            if (date == null || date.Value <= DateTime.MinValue)
                return;

            //Zum nächsten Trading Tag gehen
            var temp = date.Value.AddDays(1);

            while (temp.DayOfWeekEnum != tradingDay)
            {
                temp = temp.AddDays(1);
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
            ChartControl.Data.Add(new WLineChartFINTS(navFints) { Color = Colors.Blue, Caption = "Backtest", StrokeThickness = 0.75 });
            ChartControl.Data.Add(new WLineChartFINTS(FintsSecuritiesRepo.Eurostoxx50.Value) { Color = Colors.LightCoral, Caption = "EuroStoxx 50", StrokeThickness = 0.75 });
            ChartControl.ViewBeginDate = navFints.BeginDate;
            ChartControl.ViewEndDate = navFints.EndDate;
        }

        private void OnChartControlClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(!_model.HasPortfolioManager)
                return;

            if (ChartControl.Cursors[0]?.IsSet == false)
                return;

            var date = ChartControl.Cursors[0]?.CursorDate;
            if (date == null || date.Value <= DateTime.MinValue)
                return;

            _model.UpdateHoldings(date.Value.ToDateTime());
            _model.UpdateCash(date.Value.ToDateTime());
        }
    }
}
