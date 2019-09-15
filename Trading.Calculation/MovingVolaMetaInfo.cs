using System;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    public class MovingVolaMetaInfo : IMovingVolaMetaInfo
    {
        private readonly IPriceHistoryCollectionSettings _settings;
        private readonly int _count;

        public MovingVolaMetaInfo(decimal averageReturn, decimal variance, IPriceHistoryCollectionSettings settings, int count)
        {
            _settings = settings;
            _count = count;
            AverageReturn = averageReturn;
            Variance = variance;
            //Wurzel aus varianz/ N-1 und auf 250 Tage bringen
            DailyVolatility = (decimal)(Math.Sqrt((double)variance / (count - 1)) * Math.Sqrt(settings.MovingDaysVolatility));
        }
        /// <summary>
        /// die tägliche Volatilität
        /// </summary>
        public decimal DailyVolatility { get; }

        /// <summary>
        /// das arithmetrische MIttel der daily Returns
        /// </summary>
        public decimal AverageReturn { get; }

        /// <summary>
        /// die Varianz => Achtung ist schon durch N-1 bereiningt
        /// </summary>
        public decimal Variance { get; }


        public static MovingVolaMetaInfo Create(MovingVolaMetaInfo lastMetaInfo, ITradingRecord item, decimal firstDailyReturn, decimal lastDailyReturn, int count)
        {
            //ändere hier nur den letzen und ersten Value
            var currentAverage = lastMetaInfo.AverageReturn;
            currentAverage += lastDailyReturn * 1 / (count - 1);
            currentAverage -= firstDailyReturn * 1 / (count - 1);

            //auch bei der Varianz
            var currentVariance = lastMetaInfo.Variance;
            currentVariance += (decimal)Math.Pow(((double)currentAverage - (double)lastDailyReturn), 2);
            currentVariance -= (decimal)Math.Pow(((double)lastMetaInfo.AverageReturn - (double)firstDailyReturn), 2);

            //Trace.TraceInformation($"aktuelle Varianz: {currentVariance:N6}, aktueller Average: {currentAverage:N6}, aktuelles Datum {item.Asof}");
            //gebe dann die aktualisierte Info zurück
            return new MovingVolaMetaInfo(currentAverage, currentVariance, lastMetaInfo._settings, count);
        }
    }
}