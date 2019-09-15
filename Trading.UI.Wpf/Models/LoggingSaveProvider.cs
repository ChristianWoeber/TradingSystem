using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trading.Core.Cash;
using Trading.Core.Models;
using Trading.Core.Portfolio;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;

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
            SimpleTextParser.AppendToFile<Transaction>(args.Transaction, _stoppLossPath);
        }

        private void OnCashChangedEvent(object sender, DateTime e)
        {
            SimpleTextParser.AppendToFile<CashMetaInfo>(new CashMetaInfo(e, _pm.CashHandler.Cash), _cashPath);
        }


        private void OnPortfolioAsofChanged(object sender, DateTime e)
        {
            SimpleTextParser.AppendToFile<PortfolioValuation>(new PortfolioValuation { AllocationToRisk = _pm.AllocationToRisk, PortfolioAsof = e, PortfolioValue = _pm.PortfolioValue }, _portfolioValuationPath);
        }


        public void Save(IEnumerable<ITransaction> items)
        {
            SimpleTextParser.AppendToFile<Transaction>(items.Cast<Transaction>().Where(x => x.IsTemporary && x.Cancelled != 1), _transactionsPath);
        }

        /// <summary>
        /// Methode um den Rebalance Score, swoie den Performance Score zu speichern und zu tracen
        /// </summary>
        /// <param name="temporaryCandidatesDictionary"></param>
        /// <param name="temporaryPortfolio"></param>
        public void SaveScoring(Dictionary<int, ITradingCandidate> temporaryCandidatesDictionary, ITemporaryPortfolio temporaryPortfolio)
        {
            var completePath = Path.Combine(_loggingPath, nameof(ScoringTraceModel) + ".csv");
            SimpleTextParser.AppendToFile<ScoringTraceModel>(CreateScoringTraceModels(temporaryCandidatesDictionary, temporaryPortfolio), completePath);
        }

        /// <summary>
        /// Gibt die ScoringTraceModels zum Speichern zurück
        /// </summary>
        /// <param name="temporaryCandidatesDictionary"></param>
        /// <param name="temporaryPortfolio"></param>
        /// <returns></returns>
        private IEnumerable<ScoringTraceModel> CreateScoringTraceModels(Dictionary<int, ITradingCandidate> temporaryCandidatesDictionary, ITemporaryPortfolio temporaryPortfolio)
        {
            foreach (var transaction in temporaryPortfolio)
            {
                if (!temporaryCandidatesDictionary.TryGetValue(transaction.SecurityId, out var candidate))
                    continue;
                yield return new ScoringTraceModel(transaction, candidate.ScoringResult, candidate.RebalanceScore, _pm.PortfolioAsof);
            }
        }
    }
}
