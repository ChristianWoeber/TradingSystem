using System;
using System.Collections.Generic;
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
using HelperLibrary.Util.Atrributes;
using NLog;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.Models
{
    public class LoggingSaveProvider : ISaveProvider
    {
        private string _loggingPath;
        private readonly PortfolioManager _pm;
        private readonly string _transactionsPath;
        private string _cashPath;
        private readonly string _portfolioValuationPath;
        //private readonly Logger _transactionsLogger;
        //private readonly Logger _navLogger;
        //private readonly Logger _cashLogger;

        public LoggingSaveProvider(string settingsLoggingPath, PortfolioManager pm)
        {
            _loggingPath = settingsLoggingPath;
            _pm = pm;
            _pm.PortfolioAsofChangedEvent += OnPortfolioAsofChanged;
            pm.CashHandler.CashChangedEvent += OnCashChangedEvent;

            _transactionsPath = Path.Combine(_loggingPath, nameof(Transaction) + "s.csv");
            _cashPath = Path.Combine(_loggingPath, "Cash.csv");
            _portfolioValuationPath = Path.Combine(_loggingPath, nameof(PortfolioValuation) + "s.csv");


            //clean Up
            if(File.Exists(_transactionsPath))
                File.Delete(_transactionsPath);
            if (File.Exists(_portfolioValuationPath))
                File.Delete(_portfolioValuationPath);

            //_transactionsLogger = LogManager.GetLogger("TransactionsLogger");
            //_navLogger = LogManager.GetLogger("PortfolioValueLogger");
            //_cashLogger = LogManager.GetLogger("CashLogger");

            //Write Headers
            //_navLogger.Info(typeof(PortfolioValuation).GetProperties().Where(x => x.HasAttr<InputMapping>())
            //    .OrderBy(x => x.GetCustomAttribute<InputMapping>().SortIndex)
            //    .Select(x => x.Name)
            //    .Aggregate((a, b) => a + "|" + b));

            ////TODO: Typeof Cash Start Saldo end Saldo etc...
            //_cashLogger.Info("DATE | VALUE");

            //_transactionsLogger.Info(typeof(Transaction).GetProperties().Where(x => x.HasAttr<InputMapping>())
            //    .OrderBy(x => x.GetCustomAttribute<InputMapping>().SortIndex)
            //    .Select(x => x.Name)
            //    .Aggregate((a, b) => a + "|" + b));

        }

        private void OnCashChangedEvent(object sender, DateTime e)
        {
            //_cashLogger.Info($"{e.ToShortDateString()} | {_pm.CashHandler.Cash.ToString("N", CultureInfo.InvariantCulture)}");
            //SimpleTextParser.AppendToFile();
        }


        private void OnPortfolioAsofChanged(object sender, DateTime e)
        {
            //_navLogger.Info($"{e.ToShortDateString()} | {_pm.PortfolioValue.ToString("N", CultureInfo.InvariantCulture)} | {_pm.AllocationToRisk.ToString("N", CultureInfo.InvariantCulture)}");
            SimpleTextParser.AppendToFile(new PortfolioValuation { AllocationToRisk = _pm.AllocationToRisk, PortfolioAsof = e, PortfolioValue = _pm.PortfolioValue }, _portfolioValuationPath);
        }

        //TODO: implementieren über NLOG
        public void Save(IEnumerable<ITransaction> items)
        {

            SimpleTextParser.AppendToFile(items.Cast<Transaction>().Where(x => x.IsTemporary), _transactionsPath);

            //foreach (var transaction in items.Where(x => x.IsTemporary))
            //{
            //    _transactionsLogger.Info($"{transaction.TransactionDateTime} | {transaction.SecurityId} | {transaction.Shares} | {transaction.TargetAmountEur} | " +
            //                             $"{transaction.TransactionType} | {transaction.Cancelled} | {transaction.TargetWeight} | {transaction.EffectiveWeight}" +
            //                             $" | {transaction.EffectiveAmountEur}");
            //}
        }
    }
}
