namespace Trading.DataStructures.Interfaces
{
    public interface ITransactionCalculation
    {
        int CalculateTargetShares(ITradingCandidate candidate, decimal targetAmount);

        decimal CalculateTargetAmount(ITradingCandidate candidate);

        decimal CalculateEffectiveAmountEur(ITradingCandidate candidate, int targetShares);

        decimal CalculateEffectiveWeight(decimal effectiveAmountEur);
    }
}