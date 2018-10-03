using System;
using HelperLibrary.Database.Interfaces;

namespace Trading.UI.Wpf.Models
{

    public class TradingRecord : ITradingRecord
    {
        public decimal Price { get; set; }
        public DateTime Asof { get; set; }
        public decimal AdjustedPrice { get; set; }
        public int SecurityId { get; set; }
        public string Name { get; set; }
    }
}
