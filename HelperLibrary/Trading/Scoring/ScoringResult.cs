﻿using System;
using HelperLibrary.Interfaces;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading
{
    public class ScoringResult : IScoringResult
    {
        public ScoringResult()
        {

        }

        /// <summary>
        /// das Datum des Scoring Results
        /// </summary>
        public DateTime Asof { get; set; }
        public bool IsValid => Score > 1;
        public decimal Performance250 { get; set; }
        public decimal Performance90 { get; set; }
        public decimal Performance30 { get; set; }
        public decimal Performance10 { get; set; }
        //public decimal MaxDrawdown { get; set; }
        public decimal Volatility { get; set; }

        public decimal Score
        {
            get
            {
                // Ich gewichte die performance,
                // die aktuellsten Daten haben die größten Gewichte
                //ich ziehe auch noch den maxdrawdown in der Periode ab
                var avgPerf = Performance10 * (decimal) 0.20
                              + Performance30 * (decimal) 0.30
                              + Performance90 * (decimal) 0.40
                              + Performance250 * (decimal) 0.10;
                              //+ MaxDrawdown * (decimal)0.1;

                return Math.Round((avgPerf * (1 - Volatility)) * 100, 2);

            }
        }

        public int CompareTo(object obj)
        {
            return Score.CompareTo(((IScoringResult)obj).Score);
        }
    }


    public class ConservativeScoringResult : IScoringResult
    {
        public ConservativeScoringResult()
        {

        }

        /// <summary>
        /// das Datum des Scoring Results
        /// </summary>
        public DateTime Asof { get; set; }

        /// <summary>
        /// Der Score ist nur dann valide wenn er größer als 1 ist
        /// </summary>
        public bool IsValid => Score > 1;
        public decimal Performance250 { get; set; }
        public decimal Performance90 { get; set; }
        public decimal Performance30 { get; set; }
        public decimal Performance10 { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal Volatility { get; set; }

        public decimal Score
        {
            get
            {
                // Ich gewichte die performance,
                // die mittelfirstige, bis langfristige Daten haben die größten Gewichte
                var avgPerf = Performance10 * (decimal) 0.20
                              + Performance30 * (decimal) 0.20
                              + Performance90 * (decimal) 0.40
                              + Performance250 * (decimal) 0.20;
                              //+ MaxDrawdown * (decimal)0.2;

                return Math.Round((avgPerf * (1 - Volatility)) * 100, 2);

            }
        }

        public int CompareTo(object obj)
        {
            return Score.CompareTo(((IScoringResult)obj).Score);
        }
    }

}