using System;
using System.Collections.Generic;
using HelperLibrary.Trading.PortfolioManager.Cash;

namespace Trading.UI.Wpf.ViewModels
{
    public class CashInfoCollection : Dictionary<DateTime, List<CashMetaInfo>>
    {
        private readonly int _maxTries;


        public CashInfoCollection(List<CashMetaInfo> cashMovements, int maxTries = 15)
        {
            _maxTries = maxTries;
            foreach (var cash in cashMovements)
            {
                if (!TryGetValue(cash.Asof, out var _))
                    Add(cash.Asof, new List<CashMetaInfo>());
                this[cash.Asof].Add(cash);
            }
        }


        public bool TryGetLastCash(DateTime key, out List<CashMetaInfo> cashMetaInfos)
        {
            if (TryGetValue(key, out var infos))
            {
                cashMetaInfos = infos;
                return true;
            }

            var count = 0;
            var date = key;

            while (count < _maxTries)
            {
                count++;
                if (TryGetValue(date.AddDays(-count), out var match))
                {
                    cashMetaInfos = match;
                    return true;
                }

            }
            cashMetaInfos = new List<CashMetaInfo>();
            return false;
        }
    }
}