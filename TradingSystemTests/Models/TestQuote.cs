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
    }
}