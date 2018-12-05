using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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
            if (allCandidates.Count > 0 && allCandidates[0].PortfolioAsof >= new DateTime(2000, 02, 16))
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
            }

            var mergedCandidates = investedCandidates.Count == 0
                ? allCandidates
                : investedCandidates.Union(bestCandidates).ToList();

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
                var candidate = calculatedScoreCandidatesReversed[i];
                if (_temporaryPortfolio.ContainsCandidate(candidate))
                    continue;

                if (candidate.TransactionType == TransactionType.Unchanged || candidate.TransactionType == TransactionType.Unknown)
                    continue;

                if (targetSum >= _adjustmentProvider.MinimumBoundary)
                    break;

                targetSum += candidate.TargetWeight;
                //den Kandidaten entsprechend anpassen
                _adjustmentProvider.AddToTemporaryPortfolio(candidate);
                calculatedScoreCandidatesReversed.Remove(candidate);
            }

            //Die Kandidaten die zum Abschichten zur Verfügung stehen => Nur Openings und changes die Temporär oder bestehende Investments sind, sprich schon im temporary Portfolio

            var calculatedScoreCandidates = calculatedScoreCandidatesReversed
                .Where(x => (x.TransactionType != TransactionType.Unknown || x.TransactionType != TransactionType.Close) && (x.IsTemporary || x.IsInvested))
                .OrderByDescending(x => x.RebalanceScore.Score)
                .ToList();


            //Cash bereiningen
            for (var i = calculatedScoreCandidates.Count - 1; i >= 0; i--)
            {
                var candidate = calculatedScoreCandidates[i];
                if (_temporaryPortfolio.ContainsCandidate(candidate))
                    break;

                if (_temporaryPortfolio.CurrentSumInvestedTargetWeight <= _adjustmentProvider.MaximumBoundary)
                    break;
                //den Kandidaten entsprechend anpassen
                _adjustmentProvider.AdjustTradingCandidateSell(candidate.CurrentWeight, candidate);
                _adjustmentProvider.AddToTemporaryPortfolio(candidate);
                calculatedScoreCandidates.Remove(candidate);
            }

            //Dann bestehende Investments abschichten // sonst bin ich fertig
            if (_temporaryPortfolio.CurrentSumInvestedTargetWeight > _adjustmentProvider.MaximumBoundary)
            {
                //dann muss ich die schwächsten temporären Candidaten entsprechend abschichten
                var temporaryCandidates = _adjustmentProvider.TemporaryCandidates.Values.OrderByDescending(x => x.RebalanceScore.Score).ToList();
                CashHandler.CleanUpCash(temporaryCandidates, new List<ITradingCandidate>());
            }

            if (CashHandler.Cash > 0)
                return;

            MessageBox.Show("Achtung zu wenig Cash");

            //temporären Kandidaten die ich aufstocken möchte
            //var temporaryInvestedCandidates = investedCandidates.Where(x => x.HasBetterScoring && x.TransactionType != TransactionType.Unchanged).ToList();

            ////1. die investierten Kanidaten die ich aufbauen will checken
            //AdjustInvestedCandidatesWithBetterScoring(temporaryInvestedCandidates);

            ////dann an dieser Stelle abbrechen
            //if (bestCandidates.Count == 0)
            //{
            //    CashHandler.CleanUpCash(investedCandidates, new List<ITradingCandidate>());
            //    return;
            //}

            //if (investedCandidates.Count == 0)
            //    return;

            ////die Kandiaten die zum Umschichten zur Verfügung stehen 
            //investedCandidates.RemoveAll(t => _adjustmentProvider.TemporaryCandidates.TryGetValue(t.Record.SecurityId, out _));
            ////und die potentiellen Neukäufe hinzufügen
            ////nur diejenigen die nicht dieselbe SecurityId haben
            //investedCandidates.AddRange(allCandidates.Where(t => (t.TransactionType == TransactionType.Changed || t.TransactionType == TransactionType.Open)
            //                                                     && investedCandidates.FirstOrDefault(x => x.Record.SecurityId == t.Record.SecurityId) == null));
            ////die gemergten Candiaten inklusive neuen openings
            //var mergedCandidates = investedCandidates.OrderByDescending(c => c.ScoringResult.Score).ToList();

            //for (var i = mergedCandidates.Count - 1; i >= 0; i--)
            //{
            //    for (var j = 0; j < bestCandidates.Count;)
            //    {
            //        //2. schauen ob der ersten candidate in der Liste einen besseren Score hat als der aktuelle
            //        var currentBestCandidate = bestCandidates[j];
            //        //der aktuell schlechteste Candidat den ich austauschen könnte
            //        var worstMergedCandidate = mergedCandidates[i];

            //        //wenn es den Kandidaten im Portfolio schon gibt zum nächsten
            //        if (_temporaryPortfolio.ContainsCandidate(worstMergedCandidate))
            //            break;

            //        //die Abbruchbedingung wenn der nichtinvestierte Kandiate zu schlecht ist
            //        if (currentBestCandidate.ScoringResult.Score < worstMergedCandidate.ScoringResult.Score * (1 + _settings.ReplaceBufferPct))
            //        {
            //            CashHandler.CleanUpCash(mergedCandidates, bestCandidates);
            //            return;
            //        }
            //        //sonst umschichten

            //        if (!Debugger.IsAttached)
            //        {
            //            //wenn kleiner als die mimimum holding periode weiter
            //            if (_adjustmentProvider.IsBelowMinimumHoldingPeriode(worstMergedCandidate))
            //                break;
            //        }

            //        //investierten Verkaufen
            //        _adjustmentProvider.AdjustTradingCandidateSell(worstMergedCandidate.CurrentWeight, worstMergedCandidate);
            //        _adjustmentProvider.AddToTemporaryPortfolio(worstMergedCandidate);

            //        //neuen kaufen
            //        currentBestCandidate.TransactionType = TransactionType.Open;
            //        currentBestCandidate.TargetWeight = _settings.MaximumInitialPositionSize;
            //        _adjustmentProvider.AddToTemporaryPortfolio(currentBestCandidate);
            //        //weiter gehen zum nächsten nicht investierten Kandidaten          
            //        //vorher die candidaten noch removen
            //        mergedCandidates.Remove(worstMergedCandidate);
            //        bestCandidates.Remove(currentBestCandidate);
            //        break;
            //    }
            //}
        }

        private void EnumStops(IEnumerable<ITradingCandidate> stops)
        {
            //Zuerst alle Stops exekutieren
            foreach (var stop in stops)
                _adjustmentProvider.AddToTemporaryPortfolio(stop);
        }

        private void AdjustInvestedCandidatesWithBetterScoring(List<ITradingCandidate> investedCandidates)
        {
            //1. Schauen ob es bereits temporäre transaktionen bei den investierten gibt, dann werden diese bevorzugt
            if (investedCandidates.Count == 0)
                return;
            {
                //mach das nur für temporäre mit Aufstockung, sonst kann es sein,
                // dass ich die Enumeration ändere durch den Verkauf (da setzte ich den kandidaten ja auch temporär)
                foreach (var bestTemporaryCandidate in investedCandidates)
                {
                    //Dann wurde die Position bereits verkauft und ich breake ebenfalls
                    if (_temporaryPortfolio.ContainsCandidate(bestTemporaryCandidate))
                        break;

                    //den Kandidaten entsprechend anpassen
                    _adjustmentProvider.AddToTemporaryPortfolio(bestTemporaryCandidate);

                    //wenn die summe der targetweights >1 hab ich genug kandidaten im temporären portfolio
                    if (_adjustmentProvider.TemporaryPortfolio.CurrentSumInvestedTargetWeight >= _settings.MaximumAllocationToRisk)
                        break;
                }
            }
        }
    }
}