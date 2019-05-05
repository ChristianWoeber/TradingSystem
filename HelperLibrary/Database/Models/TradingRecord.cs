using System;
using System.Data.Linq.Mapping;
using HelperLibrary.Util.Atrributes;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Database.Models
{
    public class TradingRecord : ITradingRecord
    {
        public TradingRecord()
        {

        }
        public TradingRecord(ITradingRecord record)
        {
            Asof = record.Asof;
            Price = record.Price;
            AdjustedPrice = record.AdjustedPrice;
            SecurityId = record.SecurityId;
            Name = record?.Name;
        }

        [InputMapping(KeyWords = new[] { nameof(Asof), "date", "as of" })]
        [Column(Storage = "ASOF")]
        public DateTime Asof { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Price), "close" })]
        [Column(Storage = "CLOSE_PRICE")]
        public decimal Price { get; set; }

        [InputMapping(KeyWords = new[] { nameof(SecurityId), "id", "secId" })]
        [Column(Storage = "SECURITY_ID")]
        public int SecurityId { get; set; }

        [InputMapping(KeyWords = new[] { nameof(AdjustedPrice), "adj close" })]
        [Column(Storage = "ADJUSTED_CLOSE_PRICE")]
        public decimal AdjustedPrice { get; set; }


        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Asof.ToShortDateString()} {AdjustedPrice} {Name} {SecurityId}";
        }
    }
}