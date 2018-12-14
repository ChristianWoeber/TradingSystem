using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Arts.Financial;
using Arts.WCharting;
using HelperLibrary.Database.Models;
using Trading.DataStructures.Interfaces;
using Trading.UI.Wpf.Utils;
using Trading.UI.Wpf.ViewModels;

namespace Trading.UI.Wpf.Windows
{
    /// <summary>
    /// Interaktionslogik für ChartWindow.xaml
    /// </summary>
    public partial class ChartWindow : Window
    {
        private readonly int _securityId;
        private readonly DateTime _chartDate;
        private FINTS<double> _securityFints;
        private List<Transaction> _transactions;
        private FINTS<double> _investedFints;

        public ChartWindow()
        {
            InitializeComponent();

        }

        public ChartWindow(int securityId, DateTime chartDate) : this()
        {
            _securityId = securityId;
            _chartDate = chartDate;
        }

        public async Task CreateFints(IPriceHistoryCollection priceHistoryCollection, string caption)
        {
            var task = Task.Factory.StartNew(() =>
            {
                _securityFints = FINTS.Create(priceHistoryCollection.Select(rec => new Quote<double>(new SDate(rec.Asof), (double)rec.AdjustedPrice)), caption);
                _transactions = TransactionsRepo.GetTransactions(_chartDate, _securityId).ToList();
                _investedFints = FINTS.Create(_transactions.Select(t => new Quote<double>(new SDate(t.TransactionDateTime), (double)t.TargetWeight)), "Investitionsgrad");
            });

            await task;
            AddToChart();
        }

        private void AddToChart()
        {
            ChartControl.Data.Clear();
            _investedFints.DataType = FINTSDataType.Exposure;

            //zu ChartControl hinzufügen zuerst die Aktienquote
            ChartControl.Data.Add(new WLineChartFINTS(_investedFints)
            {
                FillColor = Colors.AliceBlue,
                Color = Colors.LightBlue,
                FillMode = WLCFillMode.FillAlpha,
                FillAlpha = 0.25
            });

            ChartControl.Data.Add(new WLineChartFINTS(_securityFints) { Color = Colors.Blue, StrokeThickness = 0.75 });
            ChartControl.Cursors[0].CursorDate = _chartDate;
            ChartControl.Cursors[1].CursorDate = _chartDate.AddDays(150);
            ChartControl.ViewBeginDate = _chartDate.AddYears(-1);
            ChartControl.ViewEndDate = _chartDate.AddYears(1);
        }
    }
}
