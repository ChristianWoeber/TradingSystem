using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Calculations
{
    public class DailyReturnMetaInfo : IDailyReturnMetaInfo
    {
        public DailyReturnMetaInfo(ITradingRecord fromRecord, ITradingRecord toRecord, decimal absoluteReturn)
        {
            FromRecord = fromRecord;
            ToRecord = toRecord;
            AbsoluteReturn = absoluteReturn;
        }

        /// <summary>
        /// Der komplette Record from
        /// </summary>
        public ITradingRecord FromRecord { get; }

        /// <summary>
        /// Der komplette Record to
        /// </summary>
        public ITradingRecord ToRecord { get; }

        /// <summary>
        /// der Return 
        /// </summary>
        public decimal AbsoluteReturn { get; }

        /// <summary>Gibt eine Zeichenfolge zurück, die das aktuelle Objekt darstellt.</summary>
        /// <returns>Eine Zeichenfolge, die das aktuelle Objekt darstellt.</returns>
        public override string ToString()
        {
            return $"{FromRecord.Asof.ToShortDateString()}_{AbsoluteReturn}";
        }
    }
}