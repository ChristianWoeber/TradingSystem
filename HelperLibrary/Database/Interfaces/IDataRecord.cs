using System;

namespace HelperLibrary.Database.Interfaces
{
    /// <summary>
    /// Das interface das als Basis für den DataRecord dient
    /// </summary>
    public interface IPriceRecord
    {
        /// <summary>
        /// Der Close Price
        /// </summary>
        decimal Price { get; set; }

        /// <summary>
        /// das Price Datum
        /// </summary>
        DateTime Asof { get; set; }

        /// <summary>
        /// Der falls vorhanden, adjustierte Price
        /// </summary>
        decimal AdjustedPrice { get; set; }
    }

    public interface ITradingRecord : IPriceRecord
    {
        int SecurityId { get; set; }

        string Name { get; set; }
    }
}
