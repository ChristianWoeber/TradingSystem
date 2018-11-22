using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Calculations
{
    /// <summary>
    /// Hilfsklasse für die Berechnung der neuen Lows
    /// </summary>
    public class LowMetaInfo
    {
        /// <summary>
        /// der erste Wert der Betrachtungsperiode
        /// </summary>
        public ITradingRecord First { get; }

        /// <summary>
        /// Das Low der Betrachtungsperiode
        /// </summary>
        public ITradingRecord Low { get; }

        /// <summary>
        /// Der Letzte Wert der Betrachtungsperiode
        /// </summary>
        public ITradingRecord Last { get; }

        /// <summary>
        /// Alle Records des MovingFensters
        /// </summary>
        public List<ITradingRecord> PeriodeRecords { get; }

        /// <summary>
        /// Gibt an ob es ein neus Low gibt
        /// </summary>
        public bool HasNewLow { get; }

        /// <summary>
        /// Der Moving Average Periode
        /// </summary>
        public decimal MovingAverage { get; }

        /// <summary>
        /// Die Veränderung des Moving Averages
        /// </summary>
        public decimal MovingAverageDelta { get; set; }

        /// <summary>
        /// Gibt an ob die aktienquote erhöht oder gesenkt werden darf
        /// </summary>
        public bool CanMoveToNextStep { get; set; }


        private LowMetaInfo()
        {
            
        }

        //glätte hier die kurve
        //TODO: eigentlich gehört hier auch ein movingaverage genommen 15 Tage => brauche für die pricehistory ein setting dass ich im konstruktor mit üergebe
        private void CalcPerformance()
        {
            var first = PeriodeRecords[PeriodeRecords.Count - 16];
            CanMoveToNextStep = 1-(first.AdjustedPrice/ Last.AdjustedPrice) > 0;
        }

        public LowMetaInfo(ITradingRecord first, ITradingRecord low, ITradingRecord last, List<ITradingRecord> periodeRecords, bool hasNewLow = true) : this()
        {
            First = first;
            Low = low;
            Last = last;
            PeriodeRecords = periodeRecords;
            HasNewLow = hasNewLow;
            MovingAverage = Math.Round(PeriodeRecords.Select(x => x.AdjustedPrice).Average(), 6);
            CalcPerformance();
        }

        public LowMetaInfo(ITradingRecord first, ITradingRecord low, ITradingRecord last, LowMetaInfo lastLowMetaInfo, bool hasNewLow) : this()
        {
            First = first;
            Low = low;
            Last = last;
            PeriodeRecords = lastLowMetaInfo.PeriodeRecords;
            HasNewLow = hasNewLow;
            MovingAverage = Math.Round(PeriodeRecords.Select(x => x.AdjustedPrice).Average(), 6);
            MovingAverageDelta = 1 - lastLowMetaInfo.MovingAverage / MovingAverage;
            CalcPerformance();
        }


        public override string ToString()
        {
            return $"NewLow: {HasNewLow} LowDate: {Low.Asof.ToShortDateString()} lastDate: {Last.Asof.ToShortDateString()} firstDate: {First.Asof.ToShortDateString()}";
        }

        public void UpdatePeriodeRecords(ITradingRecord newLast)
        {
            //removeOldestItem
            PeriodeRecords.RemoveAt(0);
            //addnew one
            PeriodeRecords.Add(newLast);
        }
    }
}