using System;
using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Interfaces;

namespace Trading.Calculation
{
    public class HistogrammCollection : List<PeriodeResult>, IHistogrammCollection
    {

        public HistogrammCollection(int classCount = 5)
        {
            ClassCount = classCount;
        }

        public HistogrammCollection(IEnumerable<PeriodeResult> results, int periodeInYears, decimal relativeFrequency)
        {
            PeriodeInYears = periodeInYears;
            RelativeFrequency = relativeFrequency;
            AddRange(results);
        }

        /// <summary>
        /// das Maximum
        /// </summary>
        public IPeriodeResult Maximum => this.OrderByDescending(x => x.Performance).FirstOrDefault();

        /// <summary>
        /// Das Minimum
        /// </summary>
        public IPeriodeResult Minimum => this.OrderByDescending(x => x.Performance).LastOrDefault();

        /// <summary>
        /// Bestimmt die Klassenbreite
        /// </summary>
        public int ClassCount { get; }

        /// <summary>
        /// die Periode für die Rollierende Berechnung
        /// </summary>
        public int PeriodeInYears { get; }

        /// <summary>
        /// die Relative Häufigkeit der Klasse
        /// </summary>
        public decimal RelativeFrequency { get; }

        /// <summary>
        /// der Count der Collection
        /// </summary>
        int IHistogrammCollection.Count => this.Count;

        /// <summary>
        /// Enumeriert die aktuelle Klasse und zieht das minimum und maximum immer nach
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IHistogrammCollection> EnumHistogrammClasses()
        {
            var span = Maximum.Performance - Minimum.Performance;
            var classWidth = span / ClassCount;
            var currentMax = 0M;
            var currentMin = Minimum.Performance;
            for (var i = 0; i < ClassCount; i++)
            {
                if (currentMax == 0)
                    currentMax = Minimum.Performance + classWidth;
                else
                {
                    currentMin = currentMax;
                    currentMax += classWidth;
                }

                var result = this.OrderByDescending(x => x.Performance).Where(x => x.Performance < currentMax && x.Performance > currentMin).ToList();
                var rel = (decimal)result.Count / this.Count;
                yield return new HistogrammCollection(result, this[0].RollingPeriodeInYears, rel);
            }
        }

        IEnumerable<IHistogrammCollection> IHistogrammCollection.EnumHistogrammClasses()
        {
            throw new NotImplementedException();
        }
    }
}