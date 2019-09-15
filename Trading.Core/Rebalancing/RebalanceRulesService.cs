using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Rebalancing
{
    public class RebalanceRulesService : IRebalanceContext
    {
        private readonly IAdjustmentProvider _adjustmentProvider;
        private readonly List<IRebalanceRule> _rebalanceRules = new List<IRebalanceRule>();
        private readonly List<INeeedRebalanceRule> _needRebalanceRules = new List<INeeedRebalanceRule>();


        public RebalanceRulesService(IPortfolioSettings settings, IAdjustmentProvider adjustmentProvider)
        {
            _adjustmentProvider = adjustmentProvider;
            Settings = settings;
            Delta = new decimal(0.10);
            LoadRebalanceRules();
        }


        private void LoadRebalanceRules()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                foreach (var t in assembly.GetTypes().Where(x => x.IsClass))
                {
                    var interfaceType = t.GetInterface(nameof(IRebalanceRule)) ?? t.GetInterface(nameof(INeeedRebalanceRule));
                    if (interfaceType == null)
                        continue;

                    //die konkrete Class initialisieren
                    var inst = Activator.CreateInstance(t);

                    switch (inst)
                    {
                        case IRebalanceRule validationRule:
                            _rebalanceRules.Add(validationRule);
                            validationRule.Context = this;
                            break;
                        case INeeedRebalanceRule needRebalancingRule:
                            _needRebalanceRules.Add(needRebalancingRule);
                            needRebalancingRule.Context = this;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Hier werden alle Rules angewendet
        /// </summary>
        /// <param name="candidates"></param>
        public void ApplyRules(IEnumerable<ITradingCandidate> candidates)
        {
            var tradingCandidates = candidates as IList<ITradingCandidate> ?? candidates.ToList();
            foreach (var candidate in tradingCandidates)
            {
                foreach (var rule in _rebalanceRules)
                {
                    rule.Apply(candidate);
                }
            }
            //die _needRebalanceRules werden erst expost angewendet da die implizit vom Rebalancing Score ausgehen
            //returne hier bei der ersten Rule die mir true zurückgibt und erstelle daraufhin eien
            foreach (var rule in _needRebalanceRules.OrderBy(x => x.SortIndex))
            {
                if (rule.Apply(tradingCandidates.OrderByDescending(x=>x.RebalanceScore.Score)))
                {
                    RebalanceCollection = new RebalanceCollection(tradingCandidates, Settings) { NeedsRebalancing = true };
                    return;

                }
            }

            RebalanceCollection = new RebalanceCollection(tradingCandidates, Settings);
        }

        /// <summary>
        /// Die Relbalance Collection
        /// </summary>
        internal RebalanceCollection RebalanceCollection { get; set; }

        /// <summary>
        /// Das Delta des Rebalance Scores => z.B: 15%
        /// </summary>
        public decimal Delta { get; }

        ////die Portfolio Settings
        public IPortfolioSettings Settings { get; }

        /// <summary>
        /// die minimum Boundary
        /// </summary>
        public decimal MinimumBoundary => _adjustmentProvider.MinimumBoundary;

        /// <summary>
        /// die maximum Boundary
        /// </summary>
        public decimal MaximumBoundary => _adjustmentProvider.MaximumBoundary;


    }
}