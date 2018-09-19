using HelperLibrary.Database.Models;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;
using System;
using System.Collections.Generic;

namespace HelperLibrary.Trading.PortfolioManager
{
    public class DefaultSaveProvider : ISaveProvider
    {
        public void Save(IEnumerable<TransactionItem> items)
        {
            throw new NotImplementedException();
        }
    }

    public class DefaultStopLossSettings : IStopLossSettings
    {

        /// <summary>
        /// Der Wert um den die Stücke reduzoert werden sollen, wenn das Scoring oder die Bwetertung abnimmt, Position aber noch im Portfolio bleibt
        /// </summary>
        public double ReductionValue => (double)1 / 2;


        /// <summary>
        /// Das Stop Loss Limt - der Wert wird bei Positionseröffnung gesetzt und dananch bei jedem Allokationslauf angepasst - trailing limit
        /// Ist der 1- OpeningPrice / CurrentPrice
        /// </summary>
        public decimal LossLimit { get; set; }

        /// <summary>
        /// Gibt zurück ob es die aktuelle Positon ausstoppt
        /// </summary>
        /// <param name="candidate">der Trading Candidate</param>
        /// <returns></returns>
        public bool HasStopLoss(TradingCandidate candidate)
        {
            //TODO: Den Average Preis noch berücksichtigen?

            if (candidate == null)
                throw new ArgumentException("Der Preis darf nicht null sein");

            if (!_limitDictionary.TryGetValue(candidate.Record.SecurityId, out var stopLossMeta))
                throw new ArgumentException("An dieser Stelle muss es ein Limit geben");

            var currentPrice = candidate.Record.AdjustedPrice;
            var volatility = candidate.ScoringResult.Volatility;

            return currentPrice < stopLossMeta.High * (1 - volatility);
        }


        public void AddOrRemoveDailyLimit(TransactionItem transactionItem)
        {
            switch (transactionItem.TransactionType)
            {
                case (int)TransactionType.Open:
                    {
                        if (!_limitDictionary.ContainsKey(transactionItem.SecurityId))
                            _limitDictionary.Add(transactionItem.SecurityId, new StopLossMeta(transactionItem.EffectiveAmountEur / transactionItem.Shares, transactionItem.TransactionDateTime));
                        break;
                    }
                case (int)TransactionType.Close:
                    _limitDictionary.Remove(transactionItem.SecurityId);
                    break;
            }
        }

        private readonly Dictionary<int, StopLossMeta> _limitDictionary = new Dictionary<int, StopLossMeta>();

        public void UpdateDailyLimits(TransactionItem transactionItem, decimal? price, DateTime asof)
        {
            if (price == null)
                throw new ArgumentException("Achtung der Preis darf nicht null sein!");

            if (!_limitDictionary.TryGetValue(transactionItem.SecurityId, out var stopLossMeta))
                _limitDictionary.Add(transactionItem.SecurityId, new StopLossMeta(price.Value, asof));
            else
            {
                if (price > stopLossMeta.High)
                {
                    //dann gibt es ein neues High von dem aus ich die Stop Loss Grenze berechne
                    _limitDictionary[transactionItem.SecurityId] = new StopLossMeta(price.Value, asof);
                }
            }
        }
    }

    internal class StopLossMeta
    {
        public readonly decimal High;
        public readonly DateTime PortfolioAsof;

        public StopLossMeta(decimal price, DateTime asof)
        {
            High = price;
            PortfolioAsof = asof;
        }
    }

    public class DefaultPortfolioSettings : IPortfolioSettings
    {
        public DefaultPortfolioSettings()
        {
            InitialCashValue = Settings.Default.PortfolioValueInitial;
        }

        /// <summary>
        /// Die maximale Initiale Positionsgröße - 10% wenn noch kein Bestand in der Position, dann wird initial eine 10% Positoneröffnet - sprich nach der ersten Allokatoin sollten 10 stocks im Bestand sein
        /// </summary>
        public decimal MaximumInitialPositionSize => new decimal(0.1);

        /// <summary>
        /// Die maximale gesamte Positionsgröße - 33% - diese kann nach dem ersen aufstocken erreicht werden - 10% dann 20% dann 33%
        /// </summary>
        public decimal MaximumPositionSize => new decimal(0.33);

        /// <summary>
        /// Cash Puffer Größe 50 Bps
        /// </summary>
        public decimal CashPufferSize => new decimal(0.005);

        /// <summary>
        /// The Trading Interval of The Portfolio
        /// </summary>
        public TradingInterval Interval { get; set; } = TradingInterval.weekly;

        /// <summary>
        /// The default Trading Day in the Week
        /// </summary>
        public DayOfWeek TradingDay { get; set; } = DayOfWeek.Wednesday;

        /// <summary>
        /// der totale Investitionsgrad
        /// </summary>
        public decimal MaxTotaInvestmentLevel => (1 - (decimal)CashPufferSize);

        /// <summary>
        /// Der Initiale Portfolio Wert
        /// </summary>
        public decimal InitialCashValue { get; }
    }
}
