using System.Collections.Generic;
using HelperLibrary.Database.Models;

namespace HelperLibrary.Interfaces
{
    public interface ISaveProvider
    {
        void Save(IEnumerable<TransactionItem> items);
    }
}