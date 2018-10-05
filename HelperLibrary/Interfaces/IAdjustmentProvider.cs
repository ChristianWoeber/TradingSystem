using HelperLibrary.Trading;

namespace HelperLibrary.Interfaces
{
    public interface IAdjustmentProvider
    {
        /// <summary>
        /// Methode um anpassen des temporären portfolios
        /// </summary>
        /// <param name="candidate">der Trading candidate</param>   
        void AddToTemporaryPortfolio(TradingCandidate candidate);

        /// <summary>
        /// der CashHandler
        /// </summary>
        ICashManager CashHandler { get; }

        /// <summary>
        /// Methode um anpassen des temporären portfolios
        /// Gibt true zurück wenn das Abschichten der einen Position genug cash produziert
        /// </summary>
        /// <param name="missingCash"></param>
        /// <param name="candidate">der Trading candidate</param>
        /// <param name="adjustTargetWeightOnly"></param>
        bool AdjustTemporaryPortfolioToCashPuffer(decimal missingCash, TradingCandidate candidate, bool adjustTargetWeightOnly);
    }
}