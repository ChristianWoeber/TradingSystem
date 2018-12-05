using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Common.Lib.Extensions;
using HelperLibrary.Database.Models;
using HelperLibrary.Parsing;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Trading.PortfolioManager.Cash;
using HelperLibrary.Util.Atrributes;
using NLog;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.Models
{
    public class LoggingSaveProvider : ISaveProvider
    {
        private readonly PortfolioManager _pm;
        private readonly string _loggingPath;
        private readonly string _transactionsPath;
        private readonly string _cashPath;
        private readonly string _portfolioValuationPath;
        private readonly string _stoppLossPath;

        public LoggingSaveProvider(string settingsLoggingPath, PortfolioManager pm)
        {
            _loggingPath = settingsLoggingPath;
            _pm = pm;
            _pm.PortfolioAsofChangedEvent += OnPortfolioAsofChanged;
            pm.CashHandler.CashChangedEvent += OnCashChangedEvent;
            pm.StoppLossExecuted += OnStoppLossExecuted;

            _transactionsPath = Path.Combine(_loggingPath, nameof(Transaction) + "s.csv");
            _cashPath = Path.Combine(_loggingPath, nameof(CashMetaInfo) + "s.csv");
            _portfolioValuationPath = Path.Combine(_loggingPath, nameof(PortfolioValuation) + "s.csv");
            _stoppLossPath = Path.Combine(_loggingPath, "StoppLoss" + nameof(Transaction) + "s.csv");
          

            //clean Up
            if (File.Exists(_transactionsPath))
                File.Delete(_transactionsPath);
            if (File.Exists(_portfolioValuationPath))
                File.Delete(_portfolioValuationPath);
            if (File.Exists(_cashPath))
                File.Delete(_cashPath);
            if (File.Exists(_stoppLossPath))
                File.Delete(_stoppLossPath);

        }

        private void OnStoppLossExecuted(object sender, PortfolioManagerEventArgs args)
        {
            SimpleTextParser.AppendToFile(args.Transaction, _stoppLossPath);
        }

        private void OnCashChangedEvent(object sender, DateTime e)
        {
            SimpleTextParser.AppendToFile(new CashMetaInfo(e, _pm.CashHandler.Cash), _cashPath);
        }


        private void OnPortfolioAsofChanged(object sender, DateTime e)
        {
            if (Debugger.IsAttached && e == new DateTime(2001, 01, 17))
            {

            }
            SimpleTextParser.AppendToFile(new PortfolioValuation { AllocationToRisk = _pm.AllocationToRisk, PortfolioAsof = e, PortfolioValue = _pm.PortfolioValue }, _portfolioValuationPath);
        }


        public void Save(IEnumerable<ITransaction> items)
        {
            SimpleTextParser.AppendToFile(items.Cast<Transaction>().Where(x => x.IsTemporary && x.Cancelled != 1), _transactionsPath);
        }
    }
}
