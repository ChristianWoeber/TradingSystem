﻿using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
{
    public interface ITransactionCalculation
    {
        int CalculateTargetShares(ITradingCandidate candidate, decimal targetAmount);

        decimal CalculateTargetAmount(ITradingCandidate candidate);

        decimal CalculateEffectiveAmountEur(ITradingCandidate candidate, int targetShares);

        decimal CalculateEffectiveWeight(decimal effectiveAmountEur);
    }
}