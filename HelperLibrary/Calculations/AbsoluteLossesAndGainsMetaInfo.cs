using System;
using System.Collections.Generic;
using System.Linq;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Calculations
{
    public class AbsoluteLossesAndGainsMetaInfo : IAbsoluteLossesAndGainsMetaInfo
    {
        private readonly IPriceHistoryCollectionSettings _priceHistorySettings;
        private IDailyReturnMetaInfo _firstRecord => Records.FirstOrDefault();

        private IDailyReturnMetaInfo _lastRecord => Records.LastOrDefault();

        public decimal AbsoluteLoss { get; private set; }
        public decimal AbsoluteGain { get; private set; }
        public List<IDailyReturnMetaInfo> Records { get; }
        public decimal AbsoluteSum => AbsoluteGain + AbsoluteLoss;

        public int DaysSettings { get; }



        public AbsoluteLossesAndGainsMetaInfo(decimal absoluteLoss, decimal absoluteGain,
            IPriceHistoryCollectionSettings priceHistorySettings, List<IDailyReturnMetaInfo> records)
        {
            _priceHistorySettings = priceHistorySettings;
            AbsoluteLoss = absoluteLoss;
            AbsoluteGain = absoluteGain;
            Records = records;
            DaysSettings = _priceHistorySettings.MovingDaysAbsoluteLossesGains;
        }

        public AbsoluteLossesAndGainsMetaInfo(IAbsoluteLossesAndGainsMetaInfo absoluteLossesAndGainsMetaInfo)
        {
            Records = new List<IDailyReturnMetaInfo>(absoluteLossesAndGainsMetaInfo.Records);
            AbsoluteLoss = absoluteLossesAndGainsMetaInfo.AbsoluteLoss;
            AbsoluteGain = absoluteLossesAndGainsMetaInfo.AbsoluteGain;
            DaysSettings = absoluteLossesAndGainsMetaInfo.DaysSettings;
        }

        /// <summary>
        /// Methode zum updaten
        /// </summary>
        /// <param name="dailyReturn">die MetaInfo</param>
        public void Update(IDailyReturnMetaInfo dailyReturn)
        {
            //letzten Eintrag rausrechnen
            if (_firstRecord.AbsoluteReturn > 0)
                AbsoluteGain -= (_firstRecord.AbsoluteReturn / Records.Count);
            else
                AbsoluteLoss += (_firstRecord.AbsoluteReturn / Records.Count);


            //ersten Reinrechnen
            if (dailyReturn.AbsoluteReturn > 0)
                AbsoluteGain += (dailyReturn.AbsoluteReturn / Records.Count);
            else
            {
                AbsoluteLoss -= (_firstRecord.AbsoluteReturn / Records.Count);
            }

            //ersten eintrag löschen && letzten neu hinzufügen && zurückgeben
            Records.RemoveAt(0);
            Records.Add(dailyReturn);
        }

    }
}