using System;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Settings
{
    public class Record : IPriceRecord
    {
        /// <summary>
        /// Der Close Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// das Price Datum
        /// </summary>
        public DateTime Asof { get; set; }

        /// <summary>
        /// Der falls vorhanden, adjustierte Price
        /// </summary>
        public decimal AdjustedPrice { get; set; }

        public Record(decimal price, DateTime asof)
        {
            Price = price;
            Asof = asof;
        }

        /// <summary>Gibt eine Zeichenfolge zurück, die das aktuelle Objekt darstellt.</summary>
        /// <returns>Eine Zeichenfolge, die das aktuelle Objekt darstellt.</returns>
        public override string ToString()
        {
            return $"Price: {Price} | Asof: {Asof}";
        }
    }
}