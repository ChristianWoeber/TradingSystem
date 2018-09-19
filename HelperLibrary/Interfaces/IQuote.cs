using HelperLibrary.Database.Models;
using System;

namespace HelperLibrary.Interfaces
{
    public interface IQuote
    {
        DateTime Date { get; }
        YahooDataRecord DataRecord { get; }
    }
}
