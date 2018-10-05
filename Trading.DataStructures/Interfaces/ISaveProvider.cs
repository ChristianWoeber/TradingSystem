using System.Collections.Generic;

namespace Trading.DataStructures.Interfaces
{
    public interface ISaveProvider
    {
        void Save(IEnumerable<ITransaction> items);
    }
}