using HelperLibrary.Util.Atrributes;

namespace HelperLibrary.Database.Models
{
    public class YahooDataRecordExtended : TradingRecord
    {
        [InputMapping(KeyWords = new[] { "Name"})]
        public string Name { get; set; }
       
    }

    public class YahooDataRecordPerformanceExtended : TradingRecord
    {
        public decimal DailyChange { get; set; }

    }
}
