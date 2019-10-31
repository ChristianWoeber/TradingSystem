namespace Trading.DataStructures.Interfaces
{
    /// <summary>
    /// die Basis für den TradingCandidaten - jeder Candidate muss ein ScoringResult beinhalten
    /// </summary>
    public interface ITradingCandidateBase
    {
        /// <summary>
        /// Der Trading Record
        /// </summary>
        ITradingRecord Record { get; }

        /// <summary>
        /// das Ergebnis des Scores
        /// </summary>
        IScoringResult ScoringResult { get; }
    }
}