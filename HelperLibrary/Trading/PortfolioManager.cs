using HelperLibrary.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using HelperLibrary.Extensions;
using HelperLibrary.Database;

namespace HelperLibrary.Trading
{

    // class that holds the portfolio logic
    public class PortfolioManager : PortfolioManagerBase
    {
        /// <summary>
        /// Default Constructor for Initializing Settings from Interfaces - Injitializes default values if no argumtes are passed in
        /// </summary>
        public PortfolioManager(IStopLossSettings stopLossSettings = null, IPortfolioSettings portfolioSettings = null)
            : base(stopLossSettings ?? new DefaultStopLossSettings(), portfolioSettings ?? new DefaultPortfolioSettings())
        {

        }

        public void PassInTestTransactions(List<TransactionItem> portfolio)
        {
            _transactionsHandler.InsertTestData(portfolio);

        }

        /// <summary>
        /// the temp portfolio which holds all the changes and will be eventually stored in Database but cleard afterwards
        /// </summary>
           private Dictionary<AdjustmentType, List<TransactionItem>> _temporaryAdjustmentPortfolio { get; } = new Dictionary<AdjustmentType, List<TransactionItem>>();


        //es müsste eine List als temporäres Profolio genügen ich adde alle current Positionen und füge dann die neuen temporär hinzu
        //danach müsste es genügen sie nach scoring zu sortieren und die schlechtesten beginnend abzuschichten, so lange bis ein zulässiger investitionsgrad 
        //erreicht ist

        private readonly List<TransactionItem> _temporaryPortfolio = new List<TransactionItem>();


        public IEnumerable<TransactionItem> CurrentPortfolio => _transactionsHandler.CurrentPortfolio;

        public void PassInCandidates(IEnumerable<TradingCandidate> candidates)
        {
            _tradingCandidates.AddRange(candidates);
        }

        internal override void ApplyPortfolioRules()
        {

            //Wenn es candidaten gibt gehe ich davon aus, dass ich die Positionen kaufen will
            if (_tradingCandidates.Count > 0)
            {
                //check Candidates
                foreach (var candidate in _tradingCandidates)
                {
                    // if Position already exists
                    if (_transactionsHandler.IsActiveInvestment(candidate))
                    {
                        var currentWeight = _transactionsHandler.GetWeight(candidate);

                        //Position wurde bereits einmal mit Target 10% eröffnet
                        if (currentWeight.IsBetween(new decimal(0.08), new decimal(0.18)))
                        {
                            //wird nun auf 20% aufgestockt
                            AdjustTemporaryPortfolio(_portfolioSettings.MaximumInitialPositionSize * 2, AdjustmentType.Increment, candidate);

                        }
                        //schon einaml aufgestockt
                        else if (currentWeight.IsBetween(new decimal(0.18), new decimal(0.28)))
                        {
                            //wird auf den maximal Wert aufgestockt
                            AdjustTemporaryPortfolio(_portfolioSettings.MaximumPositionSize, AdjustmentType.Increment, candidate);

                        }
                        //maximum
                        else if (currentWeight > new decimal(0.28))
                        {
                            //wird nicht mehr aufgestockt
                            //der nächst bessere Candidate wird berücksichtigt
                            continue;
                        }
                    }

                    //den aktuellen investititopnsgrad berechnen
                    var investmentGrade = (decimal)CurrentPortfolio.Sum(x => x.Weight);

                    //set current portfolio value
                    //if smaller then one then we have some cash to spend
                    if (investmentGrade < 1)
                        _portfolioSettings.PortfolioValue = Math.Round((CurrentPortfolio.Sum(x => x.AmountEur) / investmentGrade), 4, MidpointRounding.ToEven);

                    //if not compare to current portfolio and make adjustments
                    //need to rank the current invesntments to the potentially new ones


                    //Try Open new Position//
                    //  OpenPosition(candidate);
                }
            }

            EvaluateTemporaryPortfolio();

        }

        private void EvaluateTemporaryPortfolio()
        {
            if (_temporaryAdjustmentPortfolio.Count == 0)
                return;

            foreach (var dicEntry in _temporaryAdjustmentPortfolio)
            {

            }
        }

        private void AdjustTemporaryPortfolio(float targetWeight, AdjustmentType type, TradingCandidate candidate)
        {
            switch (type)
            {
                case AdjustmentType.InitialBuy:
                    {

                    }
                    break;
                case AdjustmentType.Increment:
                    {
                        //der ziel Betrag in EuR
                        var targetAmount = _portfolioSettings.PortfolioValue * (decimal)targetWeight;

                        //müssen immer abgerundet werden
                        var targetShares = (int)Math.Round(targetAmount / candidate.Record.AdjustedPrice, 0, MidpointRounding.ToEven);

                        //transaktion erstellen
                        var transaction = new TransactionItem
                        {
                            Shares = targetShares,
                            AmountEur = targetAmount,
                            Weight = (decimal)targetWeight,
                            SecurityId = candidate.Record.SecurityId,
                            TransactionDateTime = DateTime.Today,
                            TransactionType = (int)TransactionType.Changed
                        };

                        //Add to temporary Portfolio
                        if (!_temporaryAdjustmentPortfolio.ContainsKey(type))
                            _temporaryAdjustmentPortfolio.Add(type, new List<TransactionItem>());

                        _temporaryAdjustmentPortfolio[type].Add(transaction);
                    }
                    break;
                case AdjustmentType.Sell:
                    break;
                case AdjustmentType.Hold:
                    break;
                case AdjustmentType.Exit:
                    break;
            }
        }

        private decimal GetCurrentWeight(TransactionItem currentHolding)
        {
            return currentHolding.AmountEur / _portfolioSettings.PortfolioValue;
        }
    }


    /// <summary>
    /// Interface Defining the StopLoss Settings
    /// </summary>
    public interface IStopLossSettings
    {
        double ReductionValue { get; }

        decimal LossLimit { get; set; }
    }



    /// <summary>
    /// Interface Defining the Portfolio Settings
    /// </summary>
    public interface IPortfolioSettings
    {
        float MaximumInitialPositionSize { get; }

        float MaximumPositionSize { get; }

        float CashPufferSize { get; }

        DayOfWeek TradingDay { get; set; }

        decimal PortfolioValue { get; set; }

        TradingInterval Interval { get; set; }


    }

    public enum TransactionType
    {
        Open = 1,
        Close = 2,
        Changed = 3
    }


    /// <summary>
    /// Abstrakte BasisKlasse des PortfolioManagers die, die Kandidaten als auch die TransactionItems führt
    /// </summary>
    public abstract class PortfolioManagerBase
    {

        internal readonly List<TradingCandidate> _tradingCandidates = new List<TradingCandidate>();

        internal readonly TransactionsWrapper _transactionsHandler = new TransactionsWrapper();

        internal readonly IStopLossSettings _stopLossSettings;

        internal readonly IPortfolioSettings _portfolioSettings;


        public PortfolioManagerBase(IStopLossSettings stopLossSettings, IPortfolioSettings portfolioSettings)
        {
            _stopLossSettings = stopLossSettings;
            _portfolioSettings = portfolioSettings;
        }


        /// <summary>
        /// To be implemented in the derived class
        /// </summary>
        internal abstract void ApplyPortfolioRules();
    }


    internal enum AdjustmentType
    {
        /// <summary>
        /// Initialer Kauf
        /// </summary>
        InitialBuy,
        /// <summary>
        /// Aufstockung der Position
        /// </summary>
        Increment,
        /// <summary>
        /// Reduktion der Position
        /// </summary>
        Sell,
        /// <summary>
        /// Position bleibt unverändert
        /// </summary>
        Hold,
        /// <summary>
        /// Position wird Totalverkauft
        /// </summary>
        Exit
    }

    /// <summary>
    /// Hilfsklasse für das Schreiben und lesen der kompletten Transaktionen
    /// </summary>
    public class TransactionsWrapper : IEnumerable<TransactionItem>
    {
        /// <summary>
        /// The backing storage for the transaction items
        /// </summary>
        internal static readonly Dictionary<int, List<TransactionItem>> _transactionsCache = new Dictionary<int, List<TransactionItem>>();

        /// <summary>
        /// The Current Portfolio-Holdings
        /// </summary>
        internal Portfolio CurrentPortfolio { get; } = new Portfolio(GetTransactions);


        #region Index

        public IEnumerable<TransactionItem> this[DateTime key]
        {
            get
            {
                var dateTimeDic = new Dictionary<DateTime, List<TransactionItem>>();
                foreach (var items in _transactionsCache.Values)
                {
                    foreach (var item in items)
                    {
                        if (!dateTimeDic.ContainsKey(item.TransactionDateTime))
                            dateTimeDic.Add(item.TransactionDateTime, new List<TransactionItem>());

                        dateTimeDic[item.TransactionDateTime].Add(item);
                    }
                }
                return dateTimeDic.ContainsKey(key) ? dateTimeDic[key] : null;
            }

        }


        public IEnumerable<TransactionItem> this[int key]
        {
            get => _transactionsCache.ContainsKey(key) ? _transactionsCache[key] : null;

        }
        #endregion

        #region Helper Methods

        public decimal GetWeight(TradingCandidate candidate)
        {
            return CurrentPortfolio[candidate.Record.SecurityId].Weight;
        }

        internal bool IsActiveInvestment(TradingCandidate candidate)
        {
            return CurrentPortfolio.Any(x => x.SecurityId == candidate.Record.SecurityId);
        }

        #endregion

        #region Enumerator

        public IEnumerator<TransactionItem> GetEnumerator()
        {
            return GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return _transactionsCache.Values.GetEnumerator();
        }

        #endregion

        #region Test

        internal static List<TransactionItem> GetTransactions()
        {
            var ls = new List<TransactionItem>();
            foreach (var item in _transactionsCache.Values)
            {
                ls.AddRange(item);
            }

            return ls;
        }

        internal void InsertTestData(List<TransactionItem> portfolio)
        {
            foreach (var item in portfolio)
            {
                if (!_transactionsCache.ContainsKey(item.SecurityId))
                    _transactionsCache.Add(item.SecurityId, new List<TransactionItem>());

                _transactionsCache[item.SecurityId].Add(item);
            }
        }

        #endregion


        internal class Portfolio : IEnumerable<TransactionItem>
        {
            public static bool TestMode;
            private IEnumerable<TransactionItem> _currentPortfolio = DataBaseQueryHelper.GetCurrentPortfolio();

            /// <summary>
            /// Nur zu Testzwecken
            /// </summary>
            private Func<List<TransactionItem>> _loadTestDataFunc;

            public Portfolio(Func<List<TransactionItem>> loadTestDataFunc)
            {
                _loadTestDataFunc = loadTestDataFunc;
            }


            public TransactionItem this[int key]
            {

                get
                {
                    if (_currentPortfolio == null)
                        return null;

                    var dic = _currentPortfolio.ToDictionary(x => x.SecurityId);
                    return dic.ContainsKey(key) ? dic[key] : null;
                }
            }

            public IEnumerator<TransactionItem> GetEnumerator()
            {
                if (!TestMode)
                    return _currentPortfolio.GetEnumerator();

                //else we are in Test Mode

                if (_currentPortfolio.Count() > 0)
                    return _currentPortfolio.GetEnumerator();

                if (_currentPortfolio == null || _currentPortfolio.Count() == 0)
                    _currentPortfolio = _loadTestDataFunc.Invoke().OrderBy(x => x.TransactionDateTime);

                var dic = new Dictionary<int, TransactionItem>();
                foreach (var item in _currentPortfolio)
                {
                    if (!dic.ContainsKey(item.SecurityId))
                        dic.Add(item.SecurityId, item);
                    else if (item.TransactionType == 2)
                        dic.Remove(item.SecurityId);
                    else
                    {
                        dic[item.SecurityId].Shares += item.Shares;
                        dic[item.SecurityId].AmountEur += item.Shares < 0 ? -item.AmountEur : item.AmountEur;
                        dic[item.SecurityId].Weight += item.Shares < 0 ? -item.Weight : item.Weight;
                        dic[item.SecurityId].TransactionDateTime = item.TransactionDateTime;
                        dic[item.SecurityId].TransactionType = item.TransactionType;
                    }
                }
                _currentPortfolio = dic.Values;
                return dic.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

    }



    public class DefaultStopLossSettings : IStopLossSettings
    {
        /// <summary>
        /// Der Wert um den die Stücke reduzoert werden sollen, wenn das Scoring oder die Bwetertung abnimmt, Position aber noch im Portfolio bleibt
        /// </summary>
        public double ReductionValue => 1 / 2;


        /// <summary>
        /// Das Stop Loss Limt - der Wert wird bei Positionseröffnung gesetzt und dananch bei jedem Allokationslauf angepasst - trailing limit
        /// </summary>
        public decimal LossLimit { get; set; }
    }

    public class DefaultPortfolioSettings : IPortfolioSettings
    {
        /// <summary>
        /// Die maximale Initiale Positionsgröße - 10% wenn noch kein Bestand in der Position, dann wird initial eine 10% Positoneröffnet - sprich nach der ersten Allokatoin sollten 10 stocks im Bestand sein
        /// </summary>
        public float MaximumInitialPositionSize => 0.1f;

        /// <summary>
        /// Die maximale gesamte Positionsgröße - 33% - diese kann nach dem ersen aufstocken erreicht werden - 10% dann 20% dann 33%
        /// </summary>
        public float MaximumPositionSize => 0.33f;

        /// <summary>
        /// Cash Puffer Größe 1%
        /// </summary>
        public float CashPufferSize => 0.01f;

        /// <summary>
        /// The complete Value of the Portfolio
        /// </summary>
        public decimal PortfolioValue { get; set; } = 100000;

        /// <summary>
        /// The Trading Interval of The Portfolio
        /// </summary>
        public TradingInterval Interval { get; set; } = TradingInterval.weekly;

        /// <summary>
        /// The default Trading Day in the Week
        /// </summary>
        public DayOfWeek TradingDay { get; set; } = DayOfWeek.Wednesday;

    }


    public enum TradingInterval
    {
        /// <summary>
        /// Enum für wöchentlichen Trading Zyklus
        /// </summary>
        weekly,
        /// <summary>
        /// Enum für alle 2 Wochen Trading Zyklus
        /// </summary>
        twoWeeks,
        /// <summary>
        /// Enum für alle 3 Wochen Trading Zyklus
        /// </summary>
        threeWeeks,
        /// <summary>
        /// Enum für einmal pro Monat Trading Zyklus
        /// </summary>
        monthly,
    }
}