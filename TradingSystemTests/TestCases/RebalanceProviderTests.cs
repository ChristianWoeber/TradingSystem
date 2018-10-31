using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HelperLibrary.Database.Models;
using HelperLibrary.Parsing;
using HelperLibrary.Trading;
using HelperLibrary.Trading.PortfolioManager;
using HelperLibrary.Util.Atrributes;
using NUnit.Framework;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace TradingSystemTests.TestCases
{
    [TestFixture()]
    public class RebalanceProviderTests
    {
        private IAdjustmentProvider _adjustmentProvider;
        private PortfolioManager _dummyPortfolioManager;
        private ICashManager _cashHandler;
        private RebalanceProvider _rebalanceProvider;

        public RebalanceProviderTests()
        {
            Init();
        }

        private void Init()
        {
            _dummyPortfolioManager = new PortfolioManager(null,
                new DummyPortfolioSettings(),
                new TransactionsHandler(new DummyTransactionsCacheprovider()));

            _cashHandler = _dummyPortfolioManager.CashHandler;
            _adjustmentProvider = _dummyPortfolioManager;
            _rebalanceProvider = new RebalanceProvider(_dummyPortfolioManager.TemporaryPortfolio, _adjustmentProvider, _dummyPortfolioManager.PortfolioSettings);
        }

        [TestCase(1, 1, 1)]
        public void RebalanceTemporaryPortfolioTest(int bestCandidatesIdx, int allCandidatesIdx, int temporaryItemsIdx)
        {
            //dummy Items hinzufügen
            _dummyPortfolioManager.TemporaryPortfolio.AddRange(GetTemporaryItems(temporaryItemsIdx));

            _rebalanceProvider.RebalanceTemporaryPortfolio(GetBestTestCandidates(bestCandidatesIdx).ToList(),
                GetAllTestCandidates(allCandidatesIdx).ToList());
        }




        private readonly string[] _bestCandidates1 =
        {
            "SAP SE | Score: 38,45 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 45,75 | AveragePrice: 45,75 | HasBetterScoring: False | SecurityId: 8",
            "ORANGE | Score: 27,70 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 107,76 | AveragePrice: 107,76 | HasBetterScoring: False | SecurityId: 31",
            "ALLIANZ SE-REG | Score: 19,30 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 325,75 | AveragePrice: 325,75 | HasBetterScoring: False | SecurityId: 10",
            "VIVENDI | Score: 16,87 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 90,40 | AveragePrice: 90,40 | HasBetterScoring: False | SecurityId: 43",
            "BASF SE | Score: 12,50 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 24,50 | AveragePrice: 24,50 | HasBetterScoring: False | SecurityId: 6",
            "BAYER AG-REG | Score: 11,05 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 42,41 | AveragePrice: 42,41 | HasBetterScoring: False | SecurityId: 5",
            "SCHNEIDER ELECTRIC SE | Score: 9,07 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 37,02 | AveragePrice: 37,02 | HasBetterScoring: False | SecurityId: 35",
            "L'OREAL | Score: 8,89 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 69,90 | AveragePrice: 69,90 | HasBetterScoring: False | SecurityId: 18",
            "AIR LIQUIDE SA | Score: 7,79 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 53,79 | AveragePrice: 53,79 | HasBetterScoring: False | SecurityId: 17",
            "CRH PLC | Score: 7,23 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 19,08 | AveragePrice: 19,08 | HasBetterScoring: False | SecurityId: 41"
        };

        private readonly string[] _allCandidatesTest1 =
        {
            "SAFRAN SA | Score: 121,94 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 38,57 | AveragePrice: 29,07 | HasBetterScoring: True",
            "ASML HOLDING NV | Score: 72,25 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 40,87 | AveragePrice: 32,88 | HasBetterScoring: True",
            "NOKIA OYJ | Score: 57,05 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 44,25 | AveragePrice: 39,50 | HasBetterScoring: True",
            "TELEFONICA SA | Score: 47,76 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 23,15 | AveragePrice: 20,11 | HasBetterScoring: True",
            "SIEMENS AG - REG | Score: 40,31 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 87,50 | AveragePrice: 76,33 | HasBetterScoring: True",
            "DEUTSCHE TELEKOM AG-REG | Score: 39,51 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 69,25 | AveragePrice: 65,25 | HasBetterScoring: True",
            "KONINKLIJKE PHILIPS NV | Score: 39,17 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 37,44 | AveragePrice: 33,28 | HasBetterScoring: True",
            "SAP SE | Score: 38,45 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 45,75 | AveragePrice: 45,75 | HasBetterScoring: False",
            "LVMH MOET HENNESSY LOUIS VUI | Score: 38,09 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 88,00 | AveragePrice: 77,68 | HasBetterScoring: True",
            "MUENCHENER RUECKVER AG-REG | Score: 37,62 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 285,33 | AveragePrice: 243,48 | HasBetterScoring: True",
            "ORANGE | Score: 27,70 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 107,76 | AveragePrice: 107,76 | HasBetterScoring: False",
            "DEUTSCHE BANK AG-REGISTERED | Score: 25,08 | Invested: True | IsTemporary: True | CurrentWeight: 0,10 | TargetWeight: 0,20 | CurrentPrice: 84,80 | AveragePrice: 80,25 | HasBetterScoring: True",
            "ALLIANZ SE - REG | Score: 19,30 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 325,75 | AveragePrice: 325,75 | HasBetterScoring: False",
            "VIVENDI | Score: 16,87 | Invested: False | IsTemporary: False | CurrentWeight: 0,00 | TargetWeight: 0,00 | CurrentPrice: 90,40 | AveragePrice: 90,40 | HasBetterScoring: False"};

        private readonly Dictionary<int, string> _mappingDictionary = new Dictionary<int, string>
        {
            { 1, nameof(TradingCandidate.Name)},
            { 2, nameof(TradingCandidate.Score)},
            { 3, nameof(TradingCandidate.IsInvested)},
            { 4, nameof(TradingCandidate.IsTemporary)},
            { 5, nameof(TradingCandidate.CurrentWeight)},
            { 6, nameof(TradingCandidate.TargetWeight)},
            { 7, nameof(TradingCandidate.Record.AdjustedPrice)},
            { 8, nameof(TradingCandidate.AveragePrice)},
            { 9, nameof(TradingCandidate.HasBetterScoring)},
            { 10, nameof(TradingCandidate.SecurityId)},
        };

        private IEnumerable<ITradingCandidate> ParseTestItems(string[] items)
        {
            var sb = new StringBuilder();
            foreach (var header in _mappingDictionary.Values)
                sb.Append(header + "|");

            sb.Replace("|", "");
            sb.AppendLine();
            foreach (var item in items)
            {
                foreach (var field in item.Split('|'))
                {
                    if (!field.Contains(":"))
                        sb.Append(field);
                    else
                    {
                        var idx = field.IndexOf(':');
                        sb.Append("|");
                        sb.Append(field.Substring(idx, field.Length - idx).Trim(':'));
                    }
                }

                sb.Append("|");
                sb.AppendLine();
            }

            var list = SimpleTextParser.GetListOfType<TradingCandidateTest>(sb.ToString().TrimEnd('|'));

            foreach (var item in list)
                yield return item;
        }

        public List<ITradingCandidate> GetAllTestCandidates(int idx)
        {
            switch (idx)
            {
                case 1:
                    return ParseTestItems(_allCandidatesTest1).ToList();
                case 2:
                default:
                    break;
            }

            return null;
        }

        public List<ITradingCandidate> GetBestTestCandidates(int idx)
        {
            switch (idx)
            {
                case 1:
                    return ParseTestItems(_bestCandidates1).ToList();
                case 2:
                default:
                    break;
            }

            return null;
        }

        public List<ITransaction> GetTemporaryItems(int idx)
        {
            switch (idx)
            {
                case 1:
                    return SimpleTextParser.GetListOfType<Transaction>(
                        "TransactionDateTime;SecurityId;Shares;TargetAmountEur;TransactionType;Cancelled;TargetWeight;EffectiveWeight;EffectiveAmountEur" +
                        "05.01.2000 00:00:00; 39; 343; 10000.0; 1; 0; 0.1; 0.0997; 9971.01" +
                        "05.01.2000 00:00:00; 16; 253; 10000.0; 1; 0; 0.1; 0.0999; 9994.2586" +
                        "05.01.2000 00:00:00; 40; 304; 10000.0; 1; 0; 0.1; 0.1000; 9996.5232" +
                        "05.01.2000 00:00:00; 11; 153; 10000.0; 1; 0; 0.1; 0.0998; 9983.25  " +
                        "05.01.2000 00:00:00; 14; 497; 10000.0; 1; 0; 0.1; 0.1000; 9996.658 " +
                        "05.01.2000 00:00:00; 1; 131; 10000.0; 1; 0; 0.1; 0.1000; 9999.6230 " +
                        "05.01.2000 00:00:00; 23; 128; 10000.0; 1; 0; 0.1; 0.0994; 9943.0400" +
                        "05.01.2000 00:00:00; 27; 300; 10000.0; 1; 0; 0.1; 0.0998; 9984.6004" +
                        "05.01.2000 00:00:00; 9; 41; 10000.0; 1; 0; 0.1; 0.0998; 9982.8437  " +
                        "05.01.2000 00:00:00; 7; 124; 10000.0; 1; 0; 0.1; 0.0995; 9951.00").Select(x => (ITransaction)x).ToList();
                case 2:
                default:
                    break;
            }

            return null;
        }

    }


    [TestFixture()]
    public class AdjustmentProviderTests
    {

    }


    public class TradingCandidateTest : ITradingCandidate
    {
        [InputMapping(KeyWords = new[] { nameof(PortfolioAsof) })]
        public DateTime PortfolioAsof { get; set; }

        [InputMapping(KeyWords = new[] { nameof(LastTransaction) })]
        public ITransaction LastTransaction { get; set; }

        [InputMapping(KeyWords = new[] { nameof(CurrentPosition) })]
        public ITransaction CurrentPosition { get; set; }

        [InputMapping(KeyWords = new[] { nameof(ScoringResult) })]
        public IScoringResult ScoringResult { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Record) })]
        public ITradingRecord Record { get; set; }

        [InputMapping(KeyWords = new[] { nameof(IsInvested) })]
        public bool IsInvested { get; set; }

        [InputMapping(KeyWords = new[] { nameof(IsTemporary) })]
        public bool IsTemporary { get; set; }

        [InputMapping(KeyWords = new[] { nameof(HasStopp) })]
        public bool HasStopp { get; set; }

        [InputMapping(KeyWords = new[] { nameof(LastScoringResult) })]
        public IScoringResult LastScoringResult { get; set; }

        [InputMapping(KeyWords = new[] { nameof(TargetWeight) })]
        public decimal TargetWeight { get; set; }

        [InputMapping(KeyWords = new[] { nameof(CurrentWeight) })]
        public decimal CurrentWeight { get; set; }

        [InputMapping(KeyWords = new[] { nameof(AveragePrice) })]
        public decimal AveragePrice { get; set; }

        [InputMapping(KeyWords = new[] { nameof(TransactionType) })]
        public TransactionType TransactionType { get; set; }

        [InputMapping(KeyWords = new[] { nameof(IsBelowStopp) })]
        public bool IsBelowStopp { get; set; }

        [InputMapping(KeyWords = new[] { nameof(HasBetterScoring) })]
        public bool HasBetterScoring { get; set; }

        [InputMapping(KeyWords = new[] { nameof(SecurityId) })]
        public int SecurityId { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Name) })]
        public string Name { get; set; }


    }
}
