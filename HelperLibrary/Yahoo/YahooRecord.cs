using System;
using System.Data.Linq.Mapping;
using HelperLibrary.Util.Atrributes;

namespace HelperLibrary.Yahoo
{
    public class YahooRecord
    {
        [InputMapping(KeyWords = new string[] { "ticker", "yahooTicker" })]
        [Column(Storage = "Ticker")]
        public string Ticker { get; set; }

        [InputMapping(KeyWords = new string[] { "name", "yahooname" })]
        [Column(Storage = "NAME")]
        public string Name { get; set; }

        [InputMapping(KeyWords = new string[] { "security type" })]
        [Column(Storage = "SECURTIY_TYPE")]
        public string SecurityType { get; set; }

        [InputMapping(KeyWords = new string[] { "active type" })]
        [Column(Storage = "ACTIVE")]
        public int Active { get; set; }

        [InputMapping(KeyWords = new string[] { "id", "secId" })]
        [Column(Storage = "SECURITY_ID")]
        public int SecurityId { get; set; }

        [InputMapping(KeyWords = new string[] { "date", "per date" })]
        [Column(Storage = "ASOF")]
        public DateTime AsOf { get; set; }

        [InputMapping(KeyWords = new string[] { "close" })]
        [Column(Storage = "Close")]
        public decimal Price { get; set; }

        [InputMapping(KeyWords = new string[] { "adj close" })]
        [Column(Storage = "ADJUSTED_CLOSE_PRICE")]
        public decimal AdjClosePrice { get; set; }

        [InputMapping(KeyWords = new string[] { "Currency", "Ccy" })]
        [Column(Storage = "CURRENCY")]
        public string Currency { get; set; }
    }
}
