using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Arts.Financial;
using Arts.WCharting;
using HelperLibrary.Database.Models;
using Trading.DataStructures.Enums;
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

        public async Task CreateFints(IPriceHistoryCollection priceHistoryCollection, string caption, bool needsTransactions = true)
        {
            var task = Task.Factory.StartNew(() =>
            {
                _securityFints = FINTS.Create(priceHistoryCollection.Select(rec => new Quote<double>(new SDate(rec.Asof), (double)rec.AdjustedPrice)), caption);
                if (needsTransactions)
                {
                    _transactions = TransactionsRepo.GetTransactions(_chartDate, _securityId).ToList();
                    _investedFints = FINTS.Create(_transactions.Select(t => new Quote<double>(new SDate(t.TransactionDateTime), (double)t.TargetWeight)), "Investitionsgrad");
                }
            });

            await task;
            AddToChart();
        }

        private void AddToChart()
        {
            ChartControl.Data.Clear();
            if (_investedFints != null)
            {
                _investedFints.DataType = FINTSDataType.Exposure;

                //zu ChartControl hinzufügen zuerst die Aktienquote
                var wlineFints = new WLineChartFINTS(_investedFints)
                {
                    FillColor = Colors.OrangeRed,
                    Color = Colors.Red,
                    FillMode = WLCFillMode.FillAlpha,
                    FillAlpha = 0.25,

                };
                ChartControl.Data.Add(wlineFints);
            }

            var securityFints = new WLineChartFINTS(_securityFints) { Color = Colors.Blue, StrokeThickness = 0.75 };

            foreach (var range in EnumHighlightRanges())
            {
                securityFints.HighlightingRanges.Add(new Range<SDate>(range.Item1, range.Item2));
            }


            ChartControl.Data.Add(securityFints);
            ChartControl.Cursors[0].CursorDate = _chartDate;
            ChartControl.Cursors[1].CursorDate = _chartDate.AddDays(150);
            ChartControl.ViewBeginDate = _chartDate.AddYears(-1);
            ChartControl.ViewEndDate = _chartDate.AddYears(1);
        }

        private DateTime _currentOpen;
        private IEnumerable<Tuple<DateTime, DateTime>> EnumHighlightRanges()
        {
            if (_transactions == null)
                yield break;

            foreach (var transaction in _transactions.Where(t => t.TransactionType == TransactionType.Open || t.TransactionType == TransactionType.Close))
            {
                if (transaction.TransactionType == TransactionType.Open)
                {
                    _currentOpen = transaction.TransactionDateTime;
                }
                else if (transaction.TransactionType == TransactionType.Close)
                {
                    yield return new Tuple<DateTime, DateTime>(_currentOpen, transaction.TransactionDateTime);
                }
            }

        }
    }
}
