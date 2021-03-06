﻿using System;
using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    /// <summary>
    /// Hilfsklasse für die Berechnung der neuen Lows
    /// </summary>
    public class LowMetaInfo : ILowMetaInfo
    {
        /// <summary>
        /// der erste Wert der Betrachtungsperiode
        /// </summary>
        public ITradingRecord First { get; private set; }

        /// <summary>
        /// Das Low der Betrachtungsperiode
        /// </summary>
        public ITradingRecord Low { get; }

        /// <summary>
        /// Das High der Betrachtungsperiode
        /// </summary>
        public ITradingRecord High { get; private set; }

        /// <summary>
        /// Der Letzte Wert der Betrachtungsperiode
        /// </summary>
        public ITradingRecord Last { get; }

        /// <summary>
        /// Alle Records des MovingFensters
        /// </summary>
        public List<ITradingRecord> PeriodeRecords { get; }

        /// <summary>
        /// die Anzahl der Positiven Daily Returns in der Periode
        /// </summary>
        public IPositveDailyReturnsCollectionMetaInfo PositiveDailyRetunsMetaInfo { get; internal set; }

        /// <summary>
        /// Gibt an ob es ein neus Low gibt
        /// </summary>
        public bool HasNewLow { get; }

        /// <summary>
        /// Der Moving Average Periode
        /// </summary>
        public decimal MovingAverage { get; private set; }

        /// <summary>
        /// Die Veränderung des Moving Averages
        /// </summary>
        public decimal MovingAverageDelta { get; set; }

        /// <summary>
        /// Gibt an ob die aktienquote erhöht oder gesenkt werden darf
        /// </summary>
        public bool CanMoveToNextStep { get; set; }


        /// <summary>
        /// Ein Collection die die neuen Hochs der Periode führt
        /// und somit aussage kraft über die Trendstabilität widerspiegelt
        /// </summary>

        public ICollectionOfPeriodeHighs NewHighsCollection { get; }


        private LowMetaInfo()
        {
            NewHighsCollection = new CollectionOfPeriodeHighs();
        }

        public LowMetaInfo(ITradingRecord low, ITradingRecord last, List<ITradingRecord> periodeRecords,
            List<ITradingRecord> highs, IPositveDailyReturnsCollectionMetaInfo positiveDailyReturnsMetaInfo, bool hasNewLow = true) : this()
        {
            Low = low;
            Last = last;
            PeriodeRecords = periodeRecords;
            PositiveDailyRetunsMetaInfo = positiveDailyReturnsMetaInfo;
            First = PeriodeRecords[0];
            NewHighsCollection.AddRange(highs);
            HasNewLow = hasNewLow;
            MovingAverage = Math.Round(PeriodeRecords.Select(x => x.AdjustedPrice).Average(), 6);
            CalcPerformance();
        }

        public LowMetaInfo(ITradingRecord low, ITradingRecord last, LowMetaInfo lastLowMetaInfo,
            bool hasNewLow) : this()
        {
            Low = low;
            Last = last;
            PeriodeRecords =/* new List<ITradingRecord>(*/lastLowMetaInfo.PeriodeRecords/*)*/;
            HasNewLow = hasNewLow;
            First = lastLowMetaInfo.First;
            NewHighsCollection = new CollectionOfPeriodeHighs(lastLowMetaInfo.NewHighsCollection);
            PositiveDailyRetunsMetaInfo = lastLowMetaInfo.PositiveDailyRetunsMetaInfo;
            High = lastLowMetaInfo.High;
            MovingAverage = lastLowMetaInfo.MovingAverage;
            MovingAverageDelta = lastLowMetaInfo.MovingAverageDelta;
            CalcPerformance();
        }

        public LowMetaInfo(ITradingRecord first, ITradingRecord low, ITradingRecord last, LowMetaInfo lastLowMetaInfo, bool hasNewLow)
        {
            Low = low;
            First = first;
            Last = last;
            PeriodeRecords = /* new List<ITradingRecord>(*/lastLowMetaInfo.PeriodeRecords/*)*/;
            HasNewLow = hasNewLow;
            High = lastLowMetaInfo.High;
            MovingAverage = Math.Round(PeriodeRecords.Select(x => x.AdjustedPrice).Average(), 6);
            MovingAverageDelta = 1 - lastLowMetaInfo.MovingAverage / MovingAverage;
            CalcPerformance();
        }

        //glätte hier die kurve
        //TODO: eigentlich gehört hier auch ein movingaverage genommen 15 Tage => brauche für die pricehistory ein setting dass ich im konstruktor mit üergebe
        private void CalcPerformance()
        {
            var first = PeriodeRecords.Count < 16
                ? PeriodeRecords[0]
                : PeriodeRecords[PeriodeRecords.Count - 16];
            CanMoveToNextStep = 1 - (first.AdjustedPrice / Last.AdjustedPrice) > 0;
        }

        public void UpdateLowMetaInfo(ITradingRecord newLast)
        {
            //Wenn das High am ersten Eintrag ist und somit aus dem Timeframe fallen würde
            //muss ich es an dieser Stelle nachziehen und somit den 2-ten Record als neues High setzen
            if (High.Asof == First.Asof)
                High = PeriodeRecords[1];

            var oldMovingAverage = MovingAverage;
            //den moving average mitschleifen
            MovingAverage -= PeriodeRecords[0].AdjustedPrice / 150;
            MovingAverage += newLast.AdjustedPrice / 150;

            MovingAverageDelta = 1 - oldMovingAverage / MovingAverage;

            //den ersten Eintrag entfernen, aber nur wenn der älter oder gleich -150 Tage ist
            if (NewHighsCollection.First?.Asof <= First.Asof)
                NewHighsCollection.Shift();
            //removeOldestItem
            PeriodeRecords.RemoveAt(0);
            //addnew one
            PeriodeRecords.Add(newLast);
            //den ersten Eintrag weiterschleifen
            First = PeriodeRecords[0];
        }

        public void UpdateHigh(ITradingRecord newHigh)
        {
            High = newHigh ?? Last;
        }

        public override string ToString()
        {
            return $"NewLow: {HasNewLow} LowDate: {Low.Asof.ToShortDateString()} lastDate: {Last.Asof.ToShortDateString()} firstDate: {First.Asof.ToShortDateString()}";
        }

    }
}