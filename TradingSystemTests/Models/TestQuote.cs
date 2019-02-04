using System;
using Trading.DataStructures.Interfaces;

namespace TradingSystemTests.Models
{
    public class TestQuote : ITradingRecord
    {
        public decimal Price { get; set; }
        public DateTime Asof { get; set; }
        public decimal AdjustedPrice { get; set; }
        public int SecurityId { get; set; }
        public string Name { get; set; }

        /// <summary>Gibt eine Zeichenfolge zurück, die das aktuelle Objekt darstellt.</summary>
        /// <returns>Eine Zeichenfolge, die das aktuelle Objekt darstellt.</returns>
        public override string ToString()
        {
            return $"{Asof.ToShortDateString()}_Price:{AdjustedPrice}";
        }
    }
}