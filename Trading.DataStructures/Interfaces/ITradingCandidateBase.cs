namespace Trading.DataStructures.Interfaces
{
    public interface ITradingCandidateBase
    {
        ITradingRecord Record { get; }

        IScoringResult ScoringResult { get; }
    }
}