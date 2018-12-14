using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using HelperLibrary.Util.Converter;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Rebalancing
{
    public class RebalanceProvider : IRebalanceProvider
    {
        private readonly ITemporaryPortfolio _temporaryPortfolio;
        private readonly IAdjustmentProvider _adjustmentProvider;
        private readonly IPortfolioSettings _settings;

        public RebalanceProvider(ITemporaryPortfolio temporaryPortfolio, IAdjustmentProvider adjustmentProvider, IPortfolioSettings settings)
        {
            _temporaryPortfolio = temporaryPortfolio;
            _adjustmentProvider = adjustmentProvider;
            _settings = settings;
            CashHandler = _adjustmentProvider.CashHandler;
            RebalanceScoringProvider = new RebalanceRulesService(settings, _adjustmentProvider);
        }

        /// <summary>
        /// die Klasse kümmert sich um das Berechnen des Rebalance Scores
        /// </summary>
        public RebalanceRulesService RebalanceScoringProvider { get; }

        /// <summary>
        /// der CashManager
        /// </summary>
        public ICashManager CashHandler { get; }


        public void RebalanceTemporaryPortfolio(List<ITradingCandidate> bestCandidates, List<ITradingCandidate> allCandidates)
        {
            if (allCandidates.Count > 0 && allCandidates[0].PortfolioAsof >= new DateTime(2000, 03, 08))
            {
                //var date = allCandidates[0].PortfolioAsof;
                //JsonUtils.SerializeToFile(bestCandidates, $"BestCandidates_{date.ToShortDateString()}.txt");
                //JsonUtils.SerializeToFile(allCandidates, $"AllCandidates_{date.ToShortDateString()}.txt");
            }

            //investierte Candidaten
            var investedCandidates = allCandidates.Where(x => x.IsInvested).ToList();
            //die aktuellen stopps
            var stops = investedCandidates.Where(x => x.HasStopp && x.IsBelowStopp).ToList();

            if (stops.Count > 0)
            {
                //alle Stops entfernen
                investedCandidates.RemoveAll(candidate => stops.FirstOrDefault(s => s.Record.SecurityId == candidate.Record.SecurityId) != null);
                //Stopps exekutieren
                EnumStops(stops);
                //alle Stops entfernen
                allCandidates.RemoveAll(candidate => stops.FirstOrDefault(s => s.Record.SecurityId == candidate.Record.SecurityId) != null);
            }

            var mergedCandidates = investedCandidates.Count == 0
                ? allCandidates
                : GetMergedCandidates(bestCandidates, allCandidates, investedCandidates);

            //die Rebalance Rules anwenden
            //Hier den Rebalance Score berechnen
            RebalanceScoringProvider.ApplyRules(mergedCandidates);

            //Gibt mir an ob ich überhaupt rebalancen muss
            if (!RebalanceScoringProvider.RebalanceCollection.NeedsRebalancing)
                return;

            //ich drehe hier den sortierung und gehe die Liste reversed durch, dadruch kann ich schon ins temporäre portfolio
            //hinzugefügten Kandidaten einfach removen
            var calculatedScoreCandidatesReversed = RebalanceScoringProvider.RebalanceCollection.OrderBy(x => x.RebalanceScore.Score).ToList();

            //zusätzlich merke ich mir hier den investitionsgrad der hinzugefügten
            decimal targetSum = 0;

            for (var i = calculatedScoreCandidatesReversed.Count - 1; i >= 0; i--)
            {
                if (targetSum >= _adjustmentProvider.MinimumBoundary)
                    break;

                var candidate = calculatedScoreCandidatesReversed[i];
                if (_temporaryPortfolio.ContainsCandidate(candidate))
                    continue;

                if (candidate.TransactionType == TransactionType.Unchanged ||
                    candidate.TransactionType == TransactionType.Unknown)
                {
                    if (candidate.IsInvested)
                        targetSum += candidate.TargetWeight;
                    continue;
                }

                targetSum += candidate.TargetWeight;
                //den Kandidaten entsprechend anpassen
                _adjustmentProvider.AddToTemporaryPortfolio(candidate);
                calculatedScoreCandidatesReversed.Remove(candidate);
            }

            //Dann bestehende Investments abschichten // sonst bin ich fertig
            if (_adjustmentProvider.CurrentSumInvestedEffectiveWeight > _adjustmentProvider.MaximumBoundary)
            {
                //dann muss ich die schwächsten Candidaten entsprechend abschichten
                //und dazu merge ich die investierten mit den temporären
                var temporaryCandidates = _adjustmentProvider.TemporaryCandidates.Values.Union(allCandidates.Where(x => x.IsInvested)).OrderByDescending(x => x.RebalanceScore.Score).ToList();
                CashHandler.CleanUpCash(temporaryCandidates);
            }

            if (CashHandler.Cash > 0)
                return;

            MessageBox.Show("Achtung zu wenig Cash zum Stichtag: " + _adjustmentProvider.PortfolioAsof.ToShortDateString());

        }

        private IEnumerable<ITradingCandidate> GetMergedCandidates(List<ITradingCandidate> bestCandidates, List<ITradingCandidate> allCandidates, List<ITradingCandidate> investedCandidates)
        {
            return bestCandidates.Count > 0
                ? bestCandidates.Union(investedCandidates).OrderByDescending(x => x.RebalanceScore.Score).ToList()
                : allCandidates;
        }

        private void EnumStops(IEnumerable<ITradingCandidate> stops)
        {
            //Zuerst alle Stops exekutieren
            foreach (var stop in stops)
                _adjustmentProvider.AddToTemporaryPortfolio(stop);
        }

        //private void AdjustInvestedCandidatesWithBetterScoring(List<ITradingCandidate> investedCandidates)
        //{
        //    //1. Schauen ob es bereits temporäre transaktionen bei den investierten gibt, dann werden diese bevorzugt
        //    if (investedCandidates.Count == 0)
        //        return;
        //    {
        //        //mach das nur für temporäre mit Aufstockung, sonst kann es sein,
        //        // dass ich die Enumeration ändere durch den Verkauf (da setzte ich den kandidaten ja auch temporär)
        //        foreach (var bestTemporaryCandidate in investedCandidates)
        //        {
        //            //Dann wurde die Position bereits verkauft und ich breake ebenfalls
        //            if (_temporaryPortfolio.ContainsCandidate(bestTemporaryCandidate))
        //                break;

        //            //den Kandidaten entsprechend anpassen
        //            _adjustmentProvider.AddToTemporaryPortfolio(bestTemporaryCandidate);

        //            //wenn die summe der targetweights >1 hab ich genug kandidaten im temporären portfolio
        //            if (_adjustmentProvider.TemporaryPortfolio.CurrentSumInvestedTargetWeight >= _settings.MaximumAllocationToRisk)
        //                break;
        //        }
        //    }
        //}
    }
}