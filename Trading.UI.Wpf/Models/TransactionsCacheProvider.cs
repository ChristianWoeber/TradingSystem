using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelperLibrary.Database.Models;
using HelperLibrary.Extensions;
using HelperLibrary.Interfaces;

namespace Trading.UI.Wpf.Models
{

    public class TransactionsCacheProviderTest : ITransactionsCacheProvider
    {
        //private readonly Func<Dictionary<int, List<Transaction>>> _loadFunc;

        //private readonly HashSet<string> _uniqueTransactionsKeySet = new HashSet<string>();

        public void UpdateCache()
        {
           //empty in dem Fall
        }

        public Lazy<Dictionary<int, List<Transaction>>> TransactionsCache { get; }

        public Dictionary<DateTime, List<Transaction>> DateTimeTransactionsCache { get; }

        public TransactionsCacheProviderTest(List<Transaction> transactions)
        {
            DateTimeTransactionsCache = transactions.ToDictionaryList(x => x.TransactionDateTime);
            TransactionsCache = new Lazy<Dictionary<int, List<Transaction>>>(()=> transactions.ToDictionaryList(x=>x.SecurityId));
        }
      
    }

}
