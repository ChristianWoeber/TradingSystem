using System;
using System.Collections.Generic;
using System.Linq;
using Trading.Core.Models;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace Trading.UI.Wpf.ViewModels
{
    public class Trade
    {
        public Trade(IReadOnlyCollection<ITransaction> transactions, DateTime endDateTime)
        {
            IsValid = true;
            Opening = transactions.FirstOrDefault(x => x.TransactionType == TransactionType.Open);

            //das sollte eigentlich nie passieren
            if (Opening == null)
            {
                IsValid = false;
                Opening = transactions.FirstOrDefault();
            }

            //wenn das closing null ist bin ich Ende des Backtest, sprich dann hab ich die Position eigentlich noch im Bestand
            //wird in zukünftigen Version gefixed
            Closing = transactions.FirstOrDefault(x => x.TransactionType == TransactionType.Close) ?? Transaction.CreateCloseDummy(endDateTime, Opening);
            HoldingPeriodeInDays = (Closing?.TransactionDateTime - Opening?.TransactionDateTime)?.Days ??
                                   throw new ArgumentException($"Opening oder Closing darf nicht null sein Open: {Opening} Close:{Closing}");
            if (HoldingPeriodeInDays == 0)
            {
                IsValid = false;
                return;
            }
            var average = 0M;
            var lastTransactionDateTime = DateTime.MinValue;
            var lastTargetWeight = 0M;

            foreach (var transaction in transactions)
            {
                if (transaction.TransactionType == TransactionType.Open)
                {
                    //average += transaction.TargetWeight;
                    lastTransactionDateTime = transaction.TransactionDateTime;
                    lastTargetWeight = transaction.TargetWeight;
                }
                else
                {
                    var runningDaysInPortfolio = (transaction.TransactionDateTime - lastTransactionDateTime).Days;
                    lastTransactionDateTime = transaction.TransactionDateTime;
                    average += (runningDaysInPortfolio / (decimal)HoldingPeriodeInDays) * lastTargetWeight;
                    lastTargetWeight = transaction.TargetWeight;
                }
            }

            AveragePortfolioWeight = average;

            TotalReturn = ((Math.Abs(Closing.EffectiveAmountEur) / Math.Abs(Closing.Shares)) /
                           (Opening.EffectiveAmountEur / Opening.Shares)) - 1;

        }

        public bool IsValid { get; }

        /// <summary>
        /// die Opening Transaction des Trades
        /// </summary>
        public ITransaction Opening { get; }

        /// <summary>
        /// die Gesamtperformance im Portfolio
        /// </summary>
        public decimal TotalReturn { get; }

        /// <summary>
        /// die durchschnittliche Gewichtung im Portfolio
        /// </summary>
        public decimal AveragePortfolioWeight { get; }

        /// <summary>
        /// die HoldingPeriode in Days
        /// </summary>
        public int HoldingPeriodeInDays { get; }

        /// <summary>
        /// die Closing Transaction des Trades
        /// </summary>
        public ITransaction Closing { get; }

        /// <summary>Gibt eine Zeichenfolge zurück, die das aktuelle Objekt darstellt.</summary>
        /// <returns>Eine Zeichenfolge, die das aktuelle Objekt darstellt.</returns>
        public override string ToString()
        {
            return $"{Opening.TransactionDateTime}_{HoldingPeriodeInDays} DaysInPortfolio | TotalReturn: {TotalReturn:P} | AveragePortfolioWeight: {AveragePortfolioWeight:p}";
        }
    }
}