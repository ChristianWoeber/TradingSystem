using System;
using System.Collections.Generic;
using HelperLibrary.Database.Models;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
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
    public class DefaultStopLossSettings : IStopLossSettings
    {
        private readonly Dictionary<int, StopLossMeta> _limitDictionary = new Dictionary<int, StopLossMeta>();
        private readonly Dictionary<int, DateTime> _lastStopsDictionary = new Dictionary<int, DateTime>();

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
        /// die mindesthaltedauer nachdem ein Stop ausgelöst wurde (soll gleich am nächsten Tag wieeder ein Stop ausgelöst werden? )
        /// </summary>
        public int MinimumStopHoldingPeriodeInDays { get; set; } = 7;

        /// <summary>
        /// Gibt zurück ob es die aktuelle Position ausstoppt
        /// </summary>
        /// <param name="candidate">der Trading Candidate</param>
        /// <returns></returns>
        public bool HasStopLoss(ITradingCandidate candidate)
        {
            if (candidate == null)
                throw new ArgumentException("Der Preis darf nicht null sein");

            if (!_limitDictionary.TryGetValue(candidate.Record.SecurityId, out var stopLossMeta))
                throw new ArgumentException("An dieser Stelle muss es ein Limit geben");

            //akuteller Preis
            var currentPrice = candidate.Record.AdjustedPrice;

            //vola
            var volatility = candidate.ScoringResult.Volatility;

            //der Rückgabewert
            var hasStop = currentPrice <= stopLossMeta.High * (1 - volatility) || currentPrice <= candidate.AveragePrice * (1 - volatility);

            //sollte es einen Stop geben die lastStops updaten
            UpdateLastStops(hasStop, candidate);

            //wenn true dann setzte ich gleich das Flag beim Candidate
            return hasStop ? candidate.IsBelowStopp = true : candidate.IsBelowStopp = false;
        }

        private void UpdateLastStops(bool hasStop, ITradingCandidate candidate)
        {
            if (!hasStop)
                return;
            //wenn es schon einen Wert gibt aktualisieren
            //sonst hinzufügen
            if (_lastStopsDictionary.TryGetValue(candidate.Record.SecurityId, out var lastStopDateTime)) _lastStopsDictionary[candidate.Record.SecurityId] = candidate.Record.Asof;
            else
                _lastStopsDictionary.Add(candidate.Record.SecurityId, candidate.Record.Asof);

        }

        public bool IsBelowMinimumStopHoldingPeriod(ITradingCandidate candidate)
        {
            if (!_lastStopsDictionary.TryGetValue(candidate.Record.SecurityId, out var lastStopDateTime))
                return false;

            var days = (candidate.Record.Asof - lastStopDateTime).Days;

            if (days == 0)
                return false;
            //gebe nur true zurück wenn es schon länger als x Tage her ist, dass es die Positon ausgestoppt hat
            return days >= MinimumStopHoldingPeriodeInDays;
        }

        public void AddOrRemoveDailyLimit(ITransaction transactionItem)
        {
            //ich füge hier nur neue ein, bzw. remove bestehende Elemente
            switch (transactionItem.TransactionType)
            {
                case TransactionType.Open:
                    {
                        if (!_limitDictionary.ContainsKey(transactionItem.SecurityId))
                            _limitDictionary.Add(transactionItem.SecurityId, new StopLossMeta(transactionItem.EffectiveAmountEur / transactionItem.Shares, transactionItem.TransactionDateTime));
                        break;
                    }
                case TransactionType.Close:
                    _limitDictionary.Remove(transactionItem.SecurityId);
                    break;
            }
        }


        /// <summary>
        /// Wird bei jedem Change des Asof Datums invoked
        /// </summary>
        /// <param name="transactionItem"></param>
        /// <param name="price"></param>
        /// <param name="asof"></param>
        public void UpdateDailyLimits(ITransaction transactionItem, decimal? price, DateTime asof)
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
}