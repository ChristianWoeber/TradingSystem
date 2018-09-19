using HelperLibrary.Enums;
using HelperLibrary.Trading;

namespace HelperLibrary.Interfaces
{
    public interface IAdjustmentProvider
    {
        /// <summary>
        /// Methode um anpassen des temporären portfolios
        /// </summary>
        /// <param name="targetWeight">das zielgewicht</param>
        /// <param name="type">der transactions type</param>
        /// <param name="candidate">der Trading candidate</param>
        /// <param name="isInvested">beteits investiert</param>
        void AdjustTemporaryPortfolio(decimal targetWeight, TransactionType type, TradingCandidate candidate, bool isInvested = false);

        /// <summary>
        /// der CashHandler
        /// </summary>
        ICashManager CashHandler { get; }

        /// <summary>
        /// Methode um anpassen des temporären portfolios
        /// Gibt true zurück wenn das Abschichten der einen Position genug cash produziert
        /// </summary>
        /// <param name="missingCash"></param>
        /// <param name="type">der transactions type</param>
        /// <param name="candidate">der Trading candidate</param>
        bool AdjustTemporaryPortfolioToCashPuffer(decimal missingCash, TransactionType type, TradingCandidate candidate);
    }
}