using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Database.Models;

namespace Trading.UI.Wpf.ViewModels
{
    public static class StoppLossRepo
    {
        private static IEnumerable<Transaction> _stopps;
        private static bool _isInitialized;

        public static void Initialize(IEnumerable<Transaction> stopps)
        {
            _stopps = stopps;
            _isInitialized = true;
        }


        public static IEnumerable<Transaction> GetStops(DateTime asof)
        {
            if (!_isInitialized)
                throw new ArgumentException("Bitte vorher das Repo initialiseren");
            return _stopps.Where(x => x.TransactionDateTime <= asof);
        }
    }
}