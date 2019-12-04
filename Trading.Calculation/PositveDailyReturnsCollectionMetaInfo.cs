using System;
using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    public class PositveDailyReturnsCollectionMetaInfo : IPositveDailyReturnsCollectionMetaInfo
    {
        /// <summary>
        /// die Anzahl der positiven Returns
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// das Datum des resten Positiven Returns
        /// </summary>
        public IDailyReturnMetaInfo FirstItem { get; private set; }

        /// <summary>
        /// Die Methode shifted die klasse eine periode weiter
        /// </summary>
        /// <param name="lastDailyReturn">der neue letzte Eintrag</param>
        /// <param name="firstDailyReturn">der neue erste Eintrage</param>
        public void Shift(IDailyReturnMetaInfo lastDailyReturn, IDailyReturnMetaInfo firstDailyReturn)
        {
            //die Anpassung des letzten Eintrages
            if (lastDailyReturn.AbsoluteReturn > 0)
                Count++;
            else if (Count > 0)
                Count--;
            // die Anpassung des ersten Eintrages
            if ((firstDailyReturn.AbsoluteReturn < 0 && FirstItem.AbsoluteReturn > 0 ||
                firstDailyReturn.AbsoluteReturn > 0 && FirstItem.AbsoluteReturn > 0) && Count > 0)
                Count--;
            else if (firstDailyReturn.AbsoluteReturn > 0 && FirstItem.AbsoluteReturn < 0)
                Count++;

            FirstItem = firstDailyReturn;
        }


        /// <summary>
        /// der private Constructor, bekommt count und das Datum des ersten Positven Returns
        /// </summary>
        /// <param name="count"></param>
        /// <param name="first"></param>
        private PositveDailyReturnsCollectionMetaInfo(int count, IDailyReturnMetaInfo first)
        {
            Count = count;
            FirstItem = first;
        }

        /// <summary>
        /// erstellt eine neue MetaInfo
        /// </summary>
        /// <param name="dailyReturns"></param>
        /// <returns></returns>
        public static PositveDailyReturnsCollectionMetaInfo Create(IList<DailyReturnMetaInfo> dailyReturns)
        {
            var positiveDailyReturns = dailyReturns.Where(x => x.AbsoluteReturn > 0).ToList();
            return new PositveDailyReturnsCollectionMetaInfo(positiveDailyReturns.Count, positiveDailyReturns[0]);
        }
    }
}