using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HelperLibrary.Calculations;
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
        /// <summary>
        /// die Sorted List, die die Performance Kandidaten descending stored
        /// </summary>
        private Dictionary<int, decimal> _performanceDictionary = new Dictionary<int, decimal>();

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

            if (((TradingCandidate)candidate).SecurityId == 413740 &&
                candidate.PortfolioAsof >= new DateTime(2015, 01, 20))
            {

            }

            //akuteller Preis
            var currentPrice = candidate.Record.AdjustedPrice;

            //wenn das high kleiner gleich dem aktuellen Preis ist, kann es keinen Stopp geben
            if (stopLossMeta.High.Price <= currentPrice)
                return false;

            //wenn der letzte verkauf kürzer als 7 Tage her ist verkaufe ich nicht nochmal (StopLock Periode)
            if (candidate.LastTransaction?.Shares < 0 &&
                candidate.LastTransaction?.TransactionDateTime.AddDays(
                    _stopLossSettings.MinimumStopHoldingPeriodeInDays) > candidate.PortfolioAsof)
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
            if (currentPrice < stopLossMeta.Opening.Price * (1 - 4 * sigma) &&
                currentPrice < candidate.AveragePrice * (1 - 2 * sigma))
            {
                hasStop = true;
            }
            else if (currentPrice <= stopLossMeta.High.Price * (1 - 4 * sigma) &&
                     currentPrice < candidate.AveragePrice * (1 - 2 * sigma))
            {
                hasStop = true;
            }
            else if (currentPrice < stopLossMeta.PreviousLow.Price * (1 - 4 * sigma) &&
                     stopLossMeta.Opening.Asof != stopLossMeta.PreviousLow.Asof)
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
        /// <param name="transaction"></param>
        /// <param name="price"></param>
        /// <param name="asof"></param>
        public void UpdateDailyLimits(ITransaction transaction, decimal? price, DateTime asof)
        {
            if (price == null)
                throw new ArgumentException("Achtung der Preis darf nicht null sein!");

            if (!_limitDictionary.TryGetValue(transaction.SecurityId, out var stopLossMeta))
                _limitDictionary.Add(transaction.SecurityId, new StopLossMeta(price.Value, asof));
            else
            {
                //dann gibt es ein neues High von dem aus ich die Stop Loss Grenze berechne
                if (price > stopLossMeta.High.Price)
                {
                    //Dieser Punkt ist entscheidend, hier kann ich einmalig mein prevoius low nachziehen
                    //Komm hier nur einmal in der Phase des Highs hin
                    if (stopLossMeta.LocalLow.Asof != stopLossMeta.High.Asof)
                    {
                        _limitDictionary[transaction.SecurityId].UpdatePreviousLow(stopLossMeta.LocalLow);
                    }

                    //ich ziehe an dieser Stelle immer die das local high und das High nach
                    _limitDictionary[transaction.SecurityId].UpdateHigh(price, asof);
                    _limitDictionary[transaction.SecurityId].UpdateLocalLow(price, asof);
                }
                else
                {
                    //hier komm ich hin wenn es kein neues High gibt
                    //Wenn das lowestHigh  kleiner ist als das lowest low
                    if (price < stopLossMeta.LocalLow.Price)
                    {
                        //ziehe immer das lowest High nach
                        _limitDictionary[transaction.SecurityId].UpdateLocalLow(price, asof);
                    }
                }
            }

            if (!_performanceDictionary.TryGetValue(transaction.SecurityId, out _))
                _performanceDictionary.Add(transaction.SecurityId, CalculatePerformance());
            else
            {
                _performanceDictionary[transaction.SecurityId] = CalculatePerformance();
            }

            decimal CalculatePerformance()
            {
                if (stopLossMeta == null)
                    stopLossMeta = _limitDictionary[transaction.SecurityId];

                if (stopLossMeta.Opening.Asof == asof)
                    return 0;

                return (decimal)(price / stopLossMeta.Opening.Price - 1);
            }
        }




        /// <summary>
        /// Die bekommt die transaktion und entscheidet auf Basis des Transaktionstypen ob sie hinzugefügt oder geadded wird
        /// </summary>
        /// <param name="transaction"></param>
        public void AddOrRemoveDailyLimit(ITransaction transaction)
        {
            //ich füge hier nur neue ein, bzw. remove bestehende Elemente
            switch (transaction.TransactionType)
            {
                case TransactionType.Open:
                    {
                        if (!_limitDictionary.ContainsKey(transaction.SecurityId))
                            _limitDictionary.Add(transaction.SecurityId,
                                new StopLossMeta(transaction.EffectiveAmountEur / transaction.Shares,
                                    transaction.TransactionDateTime));
                        break;
                    }

                case TransactionType.Close:
                    _limitDictionary.Remove(transaction.SecurityId);
                    _performanceDictionary.Remove(transaction.SecurityId);
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



        /// <summary>
        /// Methode um die Performance aller holdings mit zu tracen und auch in den Rebalacne score einfließen zu lassen
        /// </summary>
        /// <param name="currentPosition">die aktuelle Postion</param>
        /// <param name="price">der Preis</param>
        /// <param name="asof">und das Portfolio Datum</param>
        public void UpdatePerformance(ITransaction currentPosition, decimal? price, DateTime asof)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// gibt on ob die aktuelle Positon zu den Top 5 der aktuell performancestärksten Positionen zählt
        /// </summary>
        /// <param name="securityId">die SecurityId</param>
        /// <param name="count">der count</param>
        /// <returns></returns>
        public bool IsUnderTopPositions(int securityId, int count = 5)
        {
            if (!_performanceDictionary.TryGetValue(securityId, out var perValue))
            {
                Trace.TraceError($"Achtung zu der SecurityId {securityId} konnte keine Performance gefunden werden");
                return false;
                //throw new ArgumentException($"Achtung zu der SecurityId {securityId} konnte keine Performance gefunden werden");
            }

            var index = _performanceDictionary.Values.ToList().IndexOf(perValue);
            return index <= count - 1;
        }

        /// <summary>
        /// Sortiert das Performacne Dictionary
        /// </summary>
        public void CreateSortedPerformanceDictionary()
        {
            _performanceDictionary = _performanceDictionary.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// gibt die Performance des Underlyings zurück
        /// </summary>
        /// <param name="securityId">der Security id</param>
        /// <returns></returns>
        public decimal? GetUnderlyingPerformance(int securityId)
        {
            return _performanceDictionary.TryGetValue(securityId, out var performanceValue) ? performanceValue : 0;
        }
    }
}