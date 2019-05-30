using System;
using System.Collections.Generic;
using HelperLibrary.Extensions;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Settings
{
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
        /// die mindesthaltedauer nachdem ein Stop ausgelöst wurde (soll gleich am nächsten Tag wieder ein Stop ausgelöst werden? )
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

            //wenn das high kleiner gleich dem aktuellen Preis ist, kann es keinen Stopp geben
            if (stopLossMeta.High.Price <= currentPrice)
                return false;

            if (candidate.LastTransaction?.Shares < 0 &&
                candidate.LastTransaction?.TransactionDateTime.AddDays(MinimumStopHoldingPeriodeInDays) < candidate.PortfolioAsof)
            {
                return false;
            }

            //die Jahres vola
            var volatility = candidate.ScoringResult.Volatility;

            //1 Sigma
            var sigma = volatility / (decimal)Math.Sqrt(250d);

            var hasStop = false;

            ////der aktuelle drawdown
            //var drawdown = (1 - stopLossMeta.High / currentPrice);


            //var hasStop = buffer - Math.Abs(drawdown) <= 0 && stopLossMeta.LowDateTime == candidate.PortfolioAsof;

            //der Rückgabewert
            //wenn der aktuelle Preis <= ist dem letzten High abzüglich der Vola stopp ich
            //wenn der aktuelle Preis kleiner als der average Preis ist sprich ich im minus bin mit der Position abzüglichn der Volatilität
            if (currentPrice < stopLossMeta.Opening.Price * (1 - 4 * sigma) && currentPrice < candidate.AveragePrice)
            {
                hasStop = true;
            }
            else if (currentPrice <= stopLossMeta.High.Price * (1 - 4 * sigma) && currentPrice < candidate.AveragePrice)
            {
                hasStop = true;
            }
            else if (currentPrice * (1 + 4 * sigma) < stopLossMeta.PreviousLow.Price && stopLossMeta.Opening.Asof != stopLossMeta.PreviousLow.Asof)
            {
                hasStop = true;
            }

            //var hasStop = currentPrice <= stopLossMeta.High.Price * (1 - (3 * sigma))
            //    && currentPrice < candidate.AveragePrice
            //    || currentPrice * (1 + sigma) < stopLossMeta.PreviousLow.Price;

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
            if (_lastStopsDictionary.TryGetValue(candidate.Record.SecurityId, out _))
                _lastStopsDictionary[candidate.Record.SecurityId] = candidate.Record.Asof;
            else
                _lastStopsDictionary.Add(candidate.Record.SecurityId, candidate.Record.Asof);

            //if (_limitDictionary.TryGetValue(candidate.Record.SecurityId, out _))
            //{
            //    //dann ziehe ich das high nach, sprich aktualisere es um den aktuellen Preis zu dem ich ausgestoppt wurde
            //    _limitDictionary[candidate.Record.SecurityId] = new StopLossMeta(candidate.Record.AdjustedPrice, candidate.PortfolioAsof);
            //}

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
                //dann gibt es ein neues High von dem aus ich die Stop Loss Grenze berechne
                if (price > stopLossMeta.High.Price)
                {
                    //Dieser Punkt ist entscheidend, hier kann ich einmalig mein prevoius low nachziehen
                    //Komm hier nur einmal in der Phase des Highs hin
                    if (stopLossMeta.LocalLow.Asof != stopLossMeta.High.Asof)
                    {
                        _limitDictionary[transactionItem.SecurityId].UpdatePreviousLow(stopLossMeta.LocalLow);
                    }

                    //ich ziehe an dieser Stelle immer die das local high und das High nach
                    _limitDictionary[transactionItem.SecurityId].UpdateHigh(price, asof);
                    _limitDictionary[transactionItem.SecurityId].UpdateLocalLow(price, asof);
                }
                else
                {
                    //hier komm ich hin wenn es kein neues High gibt
                    //Wenn das lowestHigh  kleiner ist als das lowest low
                    if (price < stopLossMeta.LocalLow.Price)
                    {
                        //ziehe immer das lowest High nach
                        _limitDictionary[transactionItem.SecurityId].UpdateLocalLow(price, asof);
                    }
                }
            }
        }
    }
}