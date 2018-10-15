using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trading.DataStructures.Interfaces;

namespace Trading.DataStructures.Utils
{
    public static class UniqueKeyProvider
    {
        public static string CreateUniqueKey(ITradingCandidate candidate)
        {
            return $"{candidate.PortfolioAsof.Date.ToShortDateString()}_{candidate.Record.SecurityId}_{(int)candidate.TransactionType}";
        }

        public static string CreateUniqueKey(ITransaction transaction)
        {
            return $"{transaction.TransactionDateTime.Date.ToShortDateString()}_{transaction.SecurityId}_{(int)transaction.TransactionType}";
        }
    }
}
