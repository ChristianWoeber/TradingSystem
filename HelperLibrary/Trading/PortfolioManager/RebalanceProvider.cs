using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HelperLibrary.Util.Converter;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager
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
        }

        /// <summary>
        /// die aktuelle Aktienquote
        /// </summary>
        public decimal CurrentAllocationToRisk => 1 - CashHandler.Cash / _adjustmentProvider.PortfolioValue;

        /// <summary>
        /// der CashManager
        /// </summary>
        public ICashManager CashHandler { get; }

        /// <summary>
        /// der aktuelle Index des Investierten Candiaten der zum Austausch zur Verfügung steht
        /// </summary>
        internal int InvestedCandidateIdx { get; set; } = 1;

        private ITradingCandidate GetNextCandiateToReplaceFromIdx(List<ITradingCandidate> investedCandidates)
        {
            if (InvestedCandidateIdx == investedCandidates.Count)
                return null;

            var worstCurrentPosition = investedCandidates[investedCandidates.Count - InvestedCandidateIdx];

            //Wenn der Kandiat noch nicht im temporären Portfolio ist kann ich ihn zurückgeben
            if (!_temporaryPortfolio.ContainsCandidate(worstCurrentPosition))
                return worstCurrentPosition;

            //sonst hat es die Position bereits ausgestoppt und ich probiere den nächsten
            InvestedCandidateIdx++;
            return GetNextCandiateToReplaceFromIdx(investedCandidates);
        }

        //private void CheckAllocationToRisk()
        //{
        //    // MaxAllocationToRisk in betracht ziehen
        //    if (PortfolioSettings.MaximumAllocationToRisk == 1)
        //        return;

        //    //Todo Implement RiskReduction
        //    var tempCandidates = TemporaryCandidates.Where(x => x.TransactionType != TransactionType.Close)
        //        .OrderBy(c => c.ScoringResult.Score).ToList();

        //    var investedCandidates = candidates.Where(x => x.IsInvested && x.TransactionType == TransactionType.Unknown)
        //        .OrderBy(x => x.ScoringResult.Score).ToList();


        //    if (PortfolioSettings.MaximumAllocationToRisk == 0)
        //    {
        //        //forcen des Totalverkaufs
        //        for (var idx = 0; idx < investedCandidates.Count; idx++)
        //        {
        //            var investedCandidate = investedCandidates[idx];
        //            if (!investedCandidate.IsTemporary)
        //            {
        //                AdjustTradingCandidateSell(decimal.Zero, investedCandidate);
        //                AddToTemporaryPortfolio(investedCandidate);
        //                investedCandidates.Remove(investedCandidate);
        //            }
        //            else
        //            {
        //                AdjustTemoraryPosition(investedCandidate);
        //                investedCandidates.Remove(investedCandidate);
        //            }
        //        }
        //    }
        //    else
        //    {

        //        while (CurrentAllocationToRisk > PortfolioSettings.MaximumAllocationToRisk)
        //        {
        //            for (var idx = 0; idx < tempCandidates.Count; idx++)
        //            {
        //                if (CurrentAllocationToRisk <= PortfolioSettings.MaximumAllocationToRisk)
        //                    break;

        //                var temporaryCandidate = tempCandidates[idx];

        //                if (!temporaryCandidate.IsTemporary)
        //                {
        //                    //wenn das Target Weight kleiner ist handelt es sich bereits um eine geplante Abschichtung => dann fliegt die Position komplett raus
        //                    AdjustTradingCandidateSell(temporaryCandidate.TargetWeight < temporaryCandidate.CurrentWeight
        //                        ? temporaryCandidate.TargetWeight
        //                        : temporaryCandidate.CurrentWeight, temporaryCandidate);

        //                    AddToTemporaryPortfolio(temporaryCandidate);
        //                }
        //                else
        //                {
        //                    AdjustTemoraryPosition(temporaryCandidate);
        //                }
        //                //sicherstellen, dass ich die Transaktion nicht 2 mal verkaufe
        //                tempCandidates.Remove(temporaryCandidate);
        //            }

        //            //dann reichen die bestehenden temporären Postion nicht aus,
        //            //dann gehe ich die aktuellen Bestände durch
        //            for (var idx = 0; idx < investedCandidates.Count; idx++)
        //            {
        //                if (CurrentAllocationToRisk <= PortfolioSettings.MaximumAllocationToRisk)
        //                    break;

        //                var investedCandidate = investedCandidates[idx];
        //                if (!investedCandidate.IsTemporary)
        //                {
        //                    AdjustTradingCandidateSell(investedCandidate.CurrentWeight, investedCandidate);
        //                    AddToTemporaryPortfolio(investedCandidate);
        //                }
        //                else
        //                {
        //                    AdjustTemoraryPosition(investedCandidate);
        //                }
        //                investedCandidates.Remove(investedCandidate);
        //            }

        //            if (tempCandidates.Count == 0 && investedCandidates.Count == 0)
        //                break;
        //        }
        //    }
        //}


        public void RebalanceTemporaryPortfolio(List<ITradingCandidate> bestCandidates, List<ITradingCandidate> allCandidates)
        {
            if (allCandidates.Count > 0 && allCandidates[0].PortfolioAsof >= new DateTime(2001, 10, 24))
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

            //temporären Kandidaten die ich aufstocken möchte
            var temporaryInvestedCandidates = investedCandidates.Where(x => x.HasBetterScoring && x.TransactionType != TransactionType.Unchanged).ToList();

            //1. die investierten Kanidaten die ich aufbauen will checken
            AdjustInvestedCandidatesWithBetterScoring(temporaryInvestedCandidates);

            //dann an dieser Stelle abbrechen
            if (bestCandidates.Count == 0)
            {
                CashHandler.CleanUpCash(investedCandidates, new List<ITradingCandidate>());
                return;
            }

            if (investedCandidates.Count == 0)
                return;

            //die Kandiaten die zum Umschichten zur Verfügung stehen 
            investedCandidates.RemoveAll(t => _adjustmentProvider.TemporaryCandidates.TryGetValue(t.Record.SecurityId, out _));
            //und die potentiellen Neukäufe hinzufügen
            //nur diejenigen die nicht dieselbe SecurityId haben
            investedCandidates.AddRange(allCandidates.Where(t => t.TransactionType == TransactionType.Open && investedCandidates.All(x => x.Record.SecurityId != t.Record.SecurityId)));
            //die gemergten Candiaten inklusive neuen openings
            var mergedCandidates = investedCandidates.OrderByDescending(c => c.ScoringResult.Score).ToList();

            for (var i = mergedCandidates.Count - 1; i >= 0; i--)
            {
                for (var j = 0; j < bestCandidates.Count;)
                {
                    //2. schauen ob der ersten candidate in der Liste einen besseren Score hat als der aktuelle
                    var currentBestCandidate = bestCandidates[j];
                    //der aktuell schlechteste Candidat den ich austauschen könnte
                    var worstMergedCandidate = mergedCandidates[i];

                    //wenn es den Kandidaten im Portfolio schon gibt zum nächsten
                    if (_temporaryPortfolio.ContainsCandidate(worstMergedCandidate))
                        break;

                    //die Abbruchbedingung wenn der nichtinvestierte Kandiate zu schlecht ist
                    if (currentBestCandidate.ScoringResult.Score < worstMergedCandidate.ScoringResult.Score * (1 + _settings.ReplaceBufferPct))
                    {
                        CashHandler.CleanUpCash(mergedCandidates, bestCandidates);
                        return;
                    }
                    //sonst umschichten

                    if (!Debugger.IsAttached)
                    {
                        //wenn kleiner als die mimimum holding periode weiter
                        if (_adjustmentProvider.IsBelowMinimumHoldingPeriode(worstMergedCandidate))
                            break;
                    }

                    //investierten Verkaufen
                    _adjustmentProvider.AdjustTradingCandidateSell(worstMergedCandidate.CurrentWeight, worstMergedCandidate);
                    _adjustmentProvider.AddToTemporaryPortfolio(worstMergedCandidate);

                    //neuen kaufen
                    currentBestCandidate.TransactionType = TransactionType.Open;
                    currentBestCandidate.TargetWeight = _settings.MaximumInitialPositionSize;
                    _adjustmentProvider.AddToTemporaryPortfolio(currentBestCandidate);
                    //weiter gehen zum nächsten nicht investierten Kandidaten          
                    //vorher die candidaten noch removen
                    mergedCandidates.Remove(worstMergedCandidate);
                    bestCandidates.Remove(currentBestCandidate);
                    break;
                }
            }


            //var nextCandidate = GetNextCandiateToReplaceFromIdx(mergedCandidates);
            //if (nextCandidate == null)
            //{
            //    CashHandler.CleanUpCash(mergedCandidates);
            //    return;
            //}

            ////wenn der beste Kandidat keinen höheren Score als der aktuell schlechteste hat brauch ich mir die anderen erst gar nicht anzusehen
            //if (best.ScoringResult.Score < nextCandidate.ScoringResult.Score * (1 + _settings.ReplaceBufferPct))
            //{
            //    CashHandler.CleanUpCash(mergedCandidates);
            //    return;
            //}

            //flag das angibt ob ich aus der foreach breaken kann
            //var isbetterCandidateLeft = true;

            ////Hier werden nur die Kandidaten ausgetauscht
            //foreach (var notInvestedCandidate in bestCandidates)
            //{
            //    //abbreuchbedingung wenn kein besser kandidat mehr in der Liste enthaltebn ist
            //    if (!isbetterCandidateLeft)
            //        break;

            //    //wenn er bereits im Temporären Portfolio ist zum nächst besseren
            //    if (notInvestedCandidate.IsTemporary)
            //    {
            //        //dann brauch ich einen neuen Kandiaten zum Abschichten
            //        InvestedCandidateIdx++;
            //        continue;
            //    }

            //    //wenn er berties investiert ist weiter
            //    if (notInvestedCandidate.IsInvested)
            //        continue;

            //    //while (InvestedCandidateIdx < investedCandidates.Count)
            //    //{
            //    //    var currentWorstInvestedCandidate = GetNextCandiateToReplaceFromIdx(investedCandidates);
            //    //    if (currentWorstInvestedCandidate == null)
            //    //        break;
            //    //    InvestedCandidateIdx++;

            //    //    if (currentWorstInvestedCandidate.HasBetterScoring || currentWorstInvestedCandidate.IsTemporary)
            //    //    {
            //    //        InvestedCandidateIdx++;
            //    //        continue;
            //    //    }

            //    //    //wenn der nicht investierte Kandidate schlechter ist als der investierte ignorieren
            //    //    //an diesem Punkt kann ich davon ausgehen, dass auch die nächsten Kandidaten nicht mehr besser sind
            //    //    // und abbrechen
            //    //    if (notInvestedCandidate.ScoringResult.Score <= currentWorstInvestedCandidate.ScoringResult.Score *
            //    //        (1 + _settings.ReplaceBufferPct))
            //    //    {
            //    //        isbetterCandidateLeft = false;
            //    //        break;
            //    //    }

            //    //    //wenn die Position bereits aufgestockt wurde tausche ich sie nicht aus
            //    //    if (currentWorstInvestedCandidate.CurrentWeight > new decimal(0.12))
            //    //        continue;

            //    //    //wenn kleiner als die mimimum holding periode weiter
            //    //    if (_adjustmentProvider.IsBelowMinimumHoldingPeriode(currentWorstInvestedCandidate))
            //    //        continue;

            //    //    //investierten Verkaufen
            //    //    _adjustmentProvider.AdjustTradingCandidateSell(currentWorstInvestedCandidate.CurrentWeight, currentWorstInvestedCandidate);
            //    //    _adjustmentProvider.AddToTemporaryPortfolio(currentWorstInvestedCandidate);

            //    //    //neuen kaufen
            //    //    notInvestedCandidate.TransactionType = TransactionType.Open;
            //    //    notInvestedCandidate.TargetWeight = _settings.MaximumInitialPositionSize;
            //    //    _adjustmentProvider.AddToTemporaryPortfolio(notInvestedCandidate);
            //    //    //weiter gehen zum nächsten nicht investierten Kandidaten                                        
            //    //    break;
            //    //}
            //}

            //clean up des Cash-Wertes
            //CashHandler.CleanUpCash(investedCandidates);
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