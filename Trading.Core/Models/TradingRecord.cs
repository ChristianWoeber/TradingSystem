using System;
using System.Data.Linq.Mapping;
using Trading.DataStructures.Interfaces;
using Trading.Parsing.Attributes;

namespace Trading.Core.Models
{
    public class TradingRecord : ITradingRecord, IInputMappable
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

        [InputMapping(KeyWords = new[] { nameof(Asof), "date", "as of" }, SortIndex = 1)]
        [Column(Storage = "ASOF")]
        public DateTime Asof { get; set; }

        [InputMapping(KeyWords = new[] { nameof(Price), "close" }, SortIndex = 3)]
        [Column(Storage = "CLOSE_PRICE")]
        public decimal Price { get; set; }

        [InputMapping(KeyWords = new[] { nameof(SecurityId), "id", "secId" }, SortIndex = 2)]
        [Column(Storage = "SECURITY_ID")]
        public int SecurityId { get; set; }

        [InputMapping(KeyWords = new[] { nameof(AdjustedPrice), "adj close" }, SortIndex = 4)]
        [Column(Storage = "ADJUSTED_CLOSE_PRICE")]
        public decimal AdjustedPrice { get; set; }


        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Asof.ToShortDateString()} {AdjustedPrice} {Name} {SecurityId}";
        }
    }
}