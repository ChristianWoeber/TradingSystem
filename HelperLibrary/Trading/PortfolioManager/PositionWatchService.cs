using System;
using System.Collections.Generic;
using HelperLibrary.Trading.PortfolioManager.Settings;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
    public class PositionWatchService : IPositionWatchService
    {
        private readonly Dictionary<int, StopLossMeta> _limitDictionary = new Dictionary<int, StopLossMeta>();
        private readonly Dictionary<int, DateTime> _lastStopsDictionary = new Dictionary<int, DateTime>();
        private readonly IStopLossSettings _stopLossSettings;

        public PositionWatchService(IStopLossSettings stopLossSettings)
        {
            _stopLossSettings = stopLossSettings;
        }

        /// <summary>
        /// Methode für das Berechnen des LossLimits
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public bool HasStopLoss(ITradingCandidate candidate)
        {
            if (candidate == null)
                throw new ArgumentException("Der Preis darf nicht null sein");

            if (!_limitDictionary.TryGetValue(candidate.Record.SecurityId, out var stopLossMeta))
                throw new ArgumentException("An dieser Stelle muss es ein Limit geben");

            if (((TradingCandidate)candidate).SecurityId == 413740 && candidate.PortfolioAsof >= new DateTime(2015, 01, 20))
            {

            }

            //akuteller Preis
            var currentPrice = candidate.Record.AdjustedPrice;

            //wenn das high kleiner gleich dem aktuellen Preis ist, kann es keinen Stopp geben
            if (stopLossMeta.High.Price <= currentPrice)
                return false;

            //wenn der letzte verkauf kürzer als 7 Tage her ist verkaufe ich nicht nochmal (StopLock Periode)
            if (candidate.LastTransaction?.Shares < 0 && candidate.LastTransaction?.TransactionDateTime.AddDays(_stopLossSettings.MinimumStopHoldingPeriodeInDays) > candidate.PortfolioAsof)
                return false;

            //die Jahres vola
            var volatility = candidate.ScoringResult.Volatility;

            //dann ist die historie noch nocht lang genug
            if (volatility == -1 || volatility == null)
            {
                //solange im Plus kein stop
                if (candidate.Performance > 0)
                    return false;

                //wenn der aktuelle Preis mehr als 20% gegenber dem letzten low gefallen ist
                return currentPrice * new decimal(0.8) > stopLossMeta.PreviousLow.Price;
            }

            //1 Sigma
            var sigma = volatility / (decimal)Math.Sqrt(250d);

            if (sigma == 0)
            {
                return currentPrice * new decimal(0.8) > stopLossMeta.Opening.Price;
            }

            var hasStop = false;

            //Wenn der Stop schon einmal abgeschictet wurde
            if (candidate.LastTransaction?.TransactionType == TransactionType.Changed &&
                candidate.LastTransaction?.Shares < 0)
            {

            }

            //der Rückgabewert
            //wenn der aktuelle Preis <= ist dem letzten High abzüglich der Vola stopp ich
            //wenn der aktuelle Preis kleiner als der average Preis ist sprich ich im minus bin mit der Position abzüglichn der Volatilität
            if (currentPrice < stopLossMeta.Opening.Price * (1 - 6 * sigma) && currentPrice < candidate.AveragePrice * (1 - 2 * sigma))
            {
                hasStop = true;
            }
            else if (currentPrice <= stopLossMeta.High.Price * (1 - 6 * sigma) && currentPrice < candidate.AveragePrice * (1 - 2 * sigma))
            {
                hasStop = true;
            }
            else if (currentPrice < stopLossMeta.PreviousLow.Price * (1 - 6 * sigma) && stopLossMeta.Opening.Asof != stopLossMeta.PreviousLow.Asof)
            {
                hasStop = true;
            }

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

        }

        /// <summary>
        /// Die Methode berechnet die Limits auf täglicher basis
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

        /// <summary>
        /// Die bekommt die transaktion und entscheidet auf Basis des Transaktionstypen ob sie hinzugefügt oder geadded wird
        /// </summary>
        /// <param name="transactionItem"></param>
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
        /// Gibt zurück ab welchem Time-Lag in tagen die Positon wider ausgestoppt werden darf 
        /// </summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        public bool IsBelowMinimumStopHoldingPeriod(ITradingCandidate candidate)
        {
            if (!_lastStopsDictionary.TryGetValue(candidate.Record.SecurityId, out var lastStopDateTime))
                return false;

            var days = (candidate.Record.Asof - lastStopDateTime).Days;

            if (days == 0)
                return false;
            //gebe nur true zurück wenn es schon länger als x Tage her ist, dass es die Positon ausgestoppt hat
            return days >= _stopLossSettings.MinimumStopHoldingPeriodeInDays;
        }


        /// <summary>
        /// Gibt zum Stichtag die zugehörige StopLossMetaInfo zurück
        /// </summary>
        /// <param name="candidate">der TradingKandiate</param>
        /// <returns></returns>
        public IStopLossMeta GetStopLossMeta(ITradingCandidate candidate)
        {
            return _limitDictionary.TryGetValue(candidate.Record.SecurityId, out var metaInfo) ? metaInfo : null;
        }
    }
}