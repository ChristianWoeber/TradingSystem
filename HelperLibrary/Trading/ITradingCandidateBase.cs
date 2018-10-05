using HelperLibrary.Database.Interfaces;
using HelperLibrary.Interfaces;

namespace HelperLibrary.Trading
{
    public interface ITradingCandidateBase
    {
        ITradingRecord Record { get; }

        IScoringResult ScoringResult { get; }
    }
}