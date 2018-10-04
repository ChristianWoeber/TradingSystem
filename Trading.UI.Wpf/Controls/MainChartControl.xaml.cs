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
            _model.BacktestCompleted += OnBacktestCompleted;
            _model.MoveCursorToNextTradingDay += OnMoveCursorToNextTradingDay;
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
            _model.UpdateHoldings(temp,true);
        }

        private void OnBacktestCompleted(object sender, BacktestResultEventArgs args)
        {
            //Fints aus dem PortfolioValue erstellen
            var navFints = FINTS.Create(args.PortfolioValuations.Select(x => new Quote<double>(new SDate(x.PortfolioAsof), (double)x.PortfolioValue)));
            var allocationFints = FINTS.Create(args.PortfolioValuations.Select(x => new Quote<double>(new SDate(x.PortfolioAsof), (double)x.AllocationToRisk)));

            //zu ChartControl hinzufügen
            ChartControl.Data.Add(new WLineChartFINTS(navFints) { FillColor = Colors.DodgerBlue, Caption = "Backtest", StrokeThickness = 0.75});
            ChartControl.Data.Add(new WLineChartFINTS(allocationFints) { FINTSDataType = FINTSDataType.Exposure, StairSteps = true, StrokeThickness = 0.50, Caption = "Investitionsgrad" });
        }

        private void OnChartControlClicked(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ChartControl.Cursors[0]?.IsSet == false)
                return;

            var date = ChartControl.Cursors[0]?.CursorDate;
            if (date == null || date.Value <= DateTime.MinValue)
                return;

            _model.UpdateHoldings(date.Value.ToDateTime());
        }
    }
}
