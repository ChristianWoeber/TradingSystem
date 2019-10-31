using System;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Scoring
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
        public decimal? Volatility { get; set; }

        public decimal Score
        {
            get
            {
                // Ich gewichte die Performance,
                // die aktuellsten Daten haben die größten Gewichte
                //ich ziehe auch noch den maxdrawdown in der Periode ab
                var avgPerf = Performance10 * (decimal)0.20
                              + Performance30 * (decimal)0.30
                              + Performance90 * (decimal)0.40
                              + Performance250 * (decimal)0.10;
                //+ MaxDrawdown * (decimal)0.1;

                //danach zinse ich quais die vola ab wenn null dann nehm ich als default 20%
                return Math.Round(avgPerf * (1 - Volatility ?? 0.2M) * 100, 2);
            }
        }

        /// <summary>
        /// die LowMetaInfo zu dem Stichtag
        /// </summary>
        public ILowMetaInfo LowMetaInfo { get; set; }

        /// <summary>
        /// Gibt an ob es positive kurz und mittelfristige Performance gibt
        /// </summary>
        public bool HasPositiveShortToMidTermPerformance => Performance10 > 0 && Performance30 > 0;

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
        /// Der Score ist nur dann valide wenn er größer als 1 ist und die Performance der letzten 10 Tage positiv ist
        /// </summary>
        public bool IsValid => Score > 1;
        public decimal Performance250 { get; set; }
        public decimal Performance90 { get; set; }
        public decimal Performance30 { get; set; }
        public decimal Performance10 { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal? Volatility { get; set; }
        public bool IsNewLow { get; set; }
        public ILowMetaInfo LowMetaInfo { get; set; }

        public bool HasPositiveShortToMidTermPerformance => Performance10 > 0 && Performance30 > 0;

        public decimal Score
        {
            get
            {
                // Ich gewichte die performance
                // die mittelfirstigen "performances", haben die größten Gewichte
                var avgPerf = Performance10 * (decimal)0.10
                              + Performance30 * (decimal)0.30
                              + Performance90 * (decimal)0.40
                              + Performance250 * (decimal)0.20;
                if (avgPerf == 0)
                    return 0;

                //die Performace um die Vola "abzinsen"
                var score = Math.Round((avgPerf * (1 - Volatility ?? 0.25M)) * 100, 2);

                if (score <= 1)
                    return score;

                //fall wenn bei der Vola ein schrott rauskommt
                if (score > 200)
                    score = -1;

                return score;
            }
        }


        public decimal ManipulateScoreFromAbsoluteLoss()
        {
            var loss = Math.Abs(AbsoluteGainAndLossMetaInfo.AbsoluteLoss);
            if (loss >= 1)
                return new decimal(0.66);
            if (loss >= new decimal(0.75))
                return new decimal(0.5);
            if (loss >= new decimal(0.5))
                return new decimal(0.35);
            if (loss >= new decimal(0.25))
                return new decimal(0.2);
            if (loss >= new decimal(0.0))
                return new decimal(0.1);

            return 1;
        }


        public IAbsoluteLossesAndGainsMetaInfo AbsoluteGainAndLossMetaInfo { get; set; }

        public int CompareTo(object obj)
        {
            return Score.CompareTo(((IScoringResult)obj).Score);
        }
    }

}
