using System;
using HelperLibrary.Util.Atrributes;
using Trading.DataStructures.Interfaces;

namespace TradingSystemTests.Models
{
    public class TestQuote : ITradingRecord
    {
        [InputMapping(KeyWords = new[] { nameof(Price), "price" })]
        public decimal Price { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Asof), "date", "asof" })]
        public DateTime Asof { get; set; }

        [InputMapping(KeyWords = new[] { nameof(AdjustedPrice), "adjPrice" })]
        public decimal AdjustedPrice { get; set; }

        [InputMapping(KeyWords = new[] { nameof(SecurityId)})]
        public int SecurityId { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Name) })]
        public string Name { get; set; }

        /// <summary>Gibt eine Zeichenfolge zurück, die das aktuelle Objekt darstellt.</summary>
        /// <returns>Eine Zeichenfolge, die das aktuelle Objekt darstellt.</returns>
        public override string ToString()
        {
            return $"{Asof.ToShortDateString()}_Price:{AdjustedPrice}";
        }
    }
}