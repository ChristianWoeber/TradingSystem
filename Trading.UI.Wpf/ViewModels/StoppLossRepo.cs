using System;
using System.Collections.Generic;
using System.Linq;
using Trading.Core.Models;
using Trading.UI.Wpf.Models;

namespace Trading.UI.Wpf.ViewModels
{
    public class StoppLossRepositoryBase<T> where T : class
    {
        private readonly IEnumerable<T> _backingStorage;

        protected StoppLossRepositoryBase(IEnumerable<T> items)
        {
            _backingStorage = items;
        }

    }

    public static class StoppLossRepository
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
    public static class ScoringRepository
    {
        private static IEnumerable<ScoringTraceModel> _scoringModels;
        private static Dictionary<string, ScoringTraceModel> _backingCache;
        private static bool _isInitialized;

        public static void Initialize(IEnumerable<ScoringTraceModel> scoring)
        {
            _scoringModels = scoring;
            _isInitialized = true;
        }

        public static ScoringTraceModel GetScore(string mappingKey)
        {
            if (!_isInitialized)
                throw new ArgumentException("Bitte vorher das Repo initialiseren");
            if (_backingCache == null)
                InitCache();

            return _backingCache.TryGetValue(mappingKey, out var scoringTraceModel) ? scoringTraceModel : null;
        }

        private static void InitCache()
        {
            _backingCache = new Dictionary<string, ScoringTraceModel>();
            foreach (var model in _scoringModels)
            {
                _backingCache.Add(model.PortfolioAsofMappingKey, model);
            }
        }
    }
}