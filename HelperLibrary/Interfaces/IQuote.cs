using System;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Interfaces
{
    public interface IQuote
    {
        DateTime Date { get; }
        ITradingRecord DataRecord { get; }
    }
}
