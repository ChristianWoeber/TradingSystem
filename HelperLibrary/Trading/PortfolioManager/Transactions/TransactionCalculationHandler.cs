using System;
using HelperLibrary.Database.Models;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Transactions
{
    /// <summary>
    /// die Methoden bauen aufeinander auf
    /// 1. Target Amount
    /// 2. Target Shares
    /// 3. Effective Amount
    /// 4. Effective Weight
    /// </summary>
    public class TransactionCalculationHandler : ITransactionCalculation
    {
        private readonly IPortfolioValuation _pm;
        private readonly IPortfolioSettings _portfolioSettings;

        public TransactionCalculationHandler(IPortfolioValuation pm, IPortfolioSettings portfolioSettings)
        {
            _pm = pm;
            _portfolioSettings = portfolioSettings;
        }

        protected decimal PortfolioValue => _pm.PortfolioValue;


        public ITransaction CreateTransaction(ITradingCandidate candidate, decimal targetAmount)
        {
            var amount = CalculateTargetAmount(candidate);
            var shares = CalculateTargetShares(candidate, amount);
            var effAmount = CalculateEffectiveAmountEur(candidate, shares);
            var effWeight = CalculateEffectiveWeight(effAmount);

            return new Transaction
            {
                Cancelled = 0,
                EffectiveWeight = effWeight,
                EffectiveAmountEur = effAmount,
                TargetWeight = candidate.TargetWeight,
                TargetAmountEur = amount,
                Shares = shares,
                TransactionDateTime = _pm.PortfolioAsof,
                TransactionType = candidate.TransactionType,
                SecurityId = candidate.Record.SecurityId,
                Name = candidate.Record.Name,
            };
        }

        public int CalculateTargetShares(ITradingCandidate candidate, decimal targetAmount)
        {
            //die gesamt ziel shares bestimmen, werden immer abgerundet
            //aussder wird bei aktivem investment nicht der target amount, sondern der
            var completeTargetShares = (int)Math.Floor(!candidate.IsInvested 
                ? targetAmount / candidate.Record.AdjustedPrice 
                : GetCurrentTargetAmount() / candidate.Record.AdjustedPrice);
            if (candidate.IsInvested)
            {
                //wenn ich investiert bin brauch ich nur die Diffenz zurückgeben
                return completeTargetShares - candidate.CurrentPosition.Shares;
            }
            return completeTargetShares;

            //interne methode gibt mir den aktuell bewerteten Totalen Amount in EUR zurück (Stücke mal heutiger Preis
            decimal GetCurrentTargetAmount() => targetAmount + candidate.CurrentPosition.Shares * candidate.Record.AdjustedPrice;
        }


        public decimal CalculateTargetAmount(ITradingCandidate candidate)
        {
            if (!ValidateCandidate(candidate))
                throw new ArgumentException($"Achtung die geplante Transaktion ist nicht valide! {candidate}");

            //der gesamt ziel Betrag in EuR
            var completeTargetAmount = Math.Round(PortfolioValue * candidate.TargetWeight, 4);

            //das darf ich nur machen wenn ich Positionen aufstocke
            if (candidate.IsInvested)
            {              
                //wenn ich investiert bin brauch ich nur die Diffenz zurückgeben
                return Math.Round(completeTargetAmount - (candidate.CurrentPosition.Shares * candidate.Record.AdjustedPrice));
            }
            return completeTargetAmount;
        }

        private bool ValidateCandidate(ITradingCandidate candidate)
        {
            //Kandidate daf nicht null sein,
            // nicht Transaktionstype unknown
            //nicht Zielgewicht 0 haben und keine LastTransaktion (kann nur verkaufen wenn ich schon Bestand habe)
            //nicht größer als 1 sein und kleiner als 0
            if (candidate == null)
                return false;

            if (candidate.TransactionType == TransactionType.Unknown)
                return false;

            if (candidate.TargetWeight == 0)
            {
                if (!candidate.IsInvested)
                    return candidate.TransactionType == TransactionType.Close;

                //Bei einem TargetWeight von 0 muss immer eine Last Transaction, eine CurrentPostion und der Transaktionstype Close sein
                if (candidate.LastTransaction == null || candidate.TransactionType != TransactionType.Close || candidate.CurrentPosition == null)
                    return false;
            }

            if (candidate.TargetWeight > 1 || candidate.TargetWeight < 0)
                return false;

            return true;
        }

        public decimal CalculateEffectiveAmountEur(ITradingCandidate candidate, int targetShares)
        {
            // das effektive gewicht
            return Math.Round(targetShares * candidate.Record.AdjustedPrice, 4);
        }

        public decimal CalculateEffectiveWeight(decimal effectiveAmountEur)
        {
            //das effektive Gewicht berechnen
            return Math.Round(effectiveAmountEur / PortfolioValue, 4);
        }
    }
}