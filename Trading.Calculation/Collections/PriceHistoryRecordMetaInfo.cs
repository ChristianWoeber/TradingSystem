using System;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation.Collections
{
    /// <summary>
    /// HilfsRecord für diverse Berechnungen innerhalb der Collection
    /// </summary>
    internal class CalculationRecordMetaInfo : ITradingRecord
    {
        public CalculationRecordMetaInfo()
        {

        }


        public CalculationRecordMetaInfo(decimal price, DateTime asof)
        {
            AdjustedPrice = price;
            Price = price;
            Asof = asof;
        }

        public CalculationRecordMetaInfo(string name, decimal price, DateTime asof) : this(price, asof)
        {
            Name = name;
        }

        public CalculationRecordMetaInfo(ITradingRecord record)
        {
            Price = record.Price;
            AdjustedPrice = record.AdjustedPrice;
            Asof = record.Asof;
            Name = record.Name;
        }




        /// <summary>
        /// Der Close Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// das Price Datum
        /// </summary>
        public DateTime Asof { get; set; }

        /// <summary>
        /// Der falls vorhanden, adjustierte Price
        /// </summary>
        public decimal AdjustedPrice { get; set; }

        /// <summary>
        /// die SecurityId
        /// </summary>
        public int SecurityId { get; set; }

        /// <summary>
        /// Der Name des Wertpapiers
        /// </summary>
        public string Name { get; set; }
    }
}