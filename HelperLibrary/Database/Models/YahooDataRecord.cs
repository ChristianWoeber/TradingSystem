using System;
using System.Data.Linq.Mapping;
using HelperLibrary.Util.Atrributes;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Database.Models
{
    public class YahooDataRecord : ITradingRecord
    {
        [InputMapping(KeyWords = new string[] { "date", "per date", "as of" })]
        [Column(Storage = "ASOF")]
        public DateTime Asof { get; set; }

        [InputMapping(KeyWords = new string[] { "close" })]
        [Column(Storage = "CLOSE_PRICE")]
        public decimal Price { get; set; }

        [InputMapping(KeyWords = new string[] { "id", "secId" })]
        [Column(Storage = "SECURITY_ID")]
        public int SecurityId { get; set; }

        [InputMapping(KeyWords = new string[] { "adj close" })]
        [Column(Storage = "ADJUSTED_CLOSE_PRICE")]
        public decimal AdjustedPrice { get; set; }


        public string Name { get; set; }
    }

    public class YahooDataRecordExtended : YahooDataRecord
    {
        [InputMapping(KeyWords = new string[] { "Name"})]
        public string Name { get; set; }

       
    }

    public class YahooDataRecordPerformanceExtended : YahooDataRecord
    {
        public decimal DailyChange { get; set; }


    }
}
