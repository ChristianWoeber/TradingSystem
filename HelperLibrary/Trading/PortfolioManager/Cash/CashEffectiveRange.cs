using System;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Cash
{
    /// <summary>
    /// Die Klasse hilft beim Adjustieren von beretis zum Temporären Portfolio hinzugefügten Positionen und stellt sicher, dass der CashWert auch wieder bereiningt wird
    /// </summary>
    internal class CashEffectiveRange : IDisposable
    {
        private readonly ITransaction _temporaryTransaction;
        private readonly ITemporaryPortfolio _temporaryPortfolio;
        
        public CashEffectiveRange(ITransaction temporaryTransaction, ITemporaryPortfolio temporaryPortfolio)
        {
            _temporaryTransaction = temporaryTransaction;
            _temporaryPortfolio = temporaryPortfolio;
            var isSell = temporaryTransaction.Shares < 0;
            if (isSell)
            {
                //dann hab ich initial beim hinzufügen das Cash als positiven CashFlow gebucht, sprich muss es nun wieder abziehen um es zu revidieren
                _temporaryPortfolio.DecrementCash(temporaryTransaction);
            }
            else
            {
                _temporaryPortfolio.IncrementCash(temporaryTransaction);
            }
        }

        public void Dispose()
        {
            var isSell = _temporaryTransaction.Shares < 0;
            if (isSell)
            {
                //im dispose wieder ganz normal CashFlow mäßig buchen
                _temporaryPortfolio.IncrementCash(_temporaryTransaction);
            }
            else
            {
                _temporaryPortfolio.DecrementCash(_temporaryTransaction);
            }
        }
    }
}