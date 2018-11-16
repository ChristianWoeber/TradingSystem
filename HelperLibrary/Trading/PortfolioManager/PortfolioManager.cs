using HelperLibrary.Database.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HelperLibrary.Extensions;
using HelperLibrary.Parsing;
using HelperLibrary.Util.Converter;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Transaction = HelperLibrary.Database.Models.Transaction;

namespace HelperLibrary.Trading.PortfolioManager
{
    public interface IRebalanceProvider
    {
        void RebalanceTemporaryPortfolio([NotNull] List<ITradingCandidate> bestCandidates, [NotNull] List<ITradingCandidate> allCandidates);
    }

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


        public void RebalanceTemporaryPortfolio(List<ITradingCandidate> bestCandidates, List<ITradingCandidate> allCandidates)
        {
            //investierte Candidaten
            var investedCandidates = allCandidates.Where(x => x.IsInvested).ToList();

            //1. die investierten Kanidaten checken
            EnumInvestedCandidates(investedCandidates);

            //dann an dieser Stelle abbrechen
            if (bestCandidates.Count == 0)
                return;

            //2. schauen ob der ersten candidate in der Liste einen besseren Score hat als der aktuelle
            //komme hier nun mit einem ausgegelichen Cash Konto her
            var best = bestCandidates[0];

            if (investedCandidates.Count == 0)
                return;

            var nextCandidate = GetNextCandiateToReplaceFromIdx(investedCandidates);
            if (nextCandidate == null)
            {
                CashHandler.CleanUpCash(allCandidates, investedCandidates);
                return;
            }

            //wenn der beste Kandidat keinen höheren Score als der aktuell schlechteste hat brauch ich mir die anderen erst gar nicht anzusehen
            if (best.ScoringResult.Score < nextCandidate.ScoringResult.Score * (1 + _settings.ReplaceBufferPct))
            {
                CashHandler.CleanUpCash(allCandidates, investedCandidates);
                return;
            }

            //flag das angibt ob ich aus der foreach breaken kann
            var isbetterCandidateLeft = true;

            //Hier werden nur die Kandidaten ausgetauscht
            foreach (var notInvestedCandidate in bestCandidates)
            {
                //abbreuchbedingung wenn kein besser kandidat mehr in der Liste enthaltebn ist
                if (!isbetterCandidateLeft)
                    break;

                //wenn er bereits im Temporären Portfolio ist zum nächst besseren
                if (notInvestedCandidate.IsTemporary)
                {
                    //dann brauch ich einen neuen Kandiaten zum Abschichten
                    InvestedCandidateIdx++;
                    continue;
                }

                //wenn er berties investiert ist weiter
                if (notInvestedCandidate.IsInvested)
                    continue;

                while (InvestedCandidateIdx < investedCandidates.Count)
                {
                    var currentWorstInvestedCandidate = GetNextCandiateToReplaceFromIdx(investedCandidates);
                    if (currentWorstInvestedCandidate == null)
                        break;
                    InvestedCandidateIdx++;

                    if (currentWorstInvestedCandidate.HasBetterScoring || currentWorstInvestedCandidate.IsTemporary)
                    {
                        InvestedCandidateIdx++;
                        continue;
                    }

                    //wenn der nicht investierte Kandidate schlechter ist als der investierte ignorieren
                    //an diesem Punkt kann ich davon ausgehen, dass auch die nächsten Kandidaten nicht mehr besser sind
                    // und abbrechen
                    if (notInvestedCandidate.ScoringResult.Score <= currentWorstInvestedCandidate.ScoringResult.Score *
                        (1 + _settings.ReplaceBufferPct))
                    {
                        isbetterCandidateLeft = false;
                        break;
                    }

                    //wenn die Position bereits aufgestockt wurde tausche ich sie nicht aus
                    if (currentWorstInvestedCandidate.CurrentWeight > new decimal(0.12))
                        continue;

                    //wenn kleiner als die mimimum holding periode weiter
                    if (_adjustmentProvider.IsBelowMinimumHoldingPeriode(currentWorstInvestedCandidate))
                        continue;

                    //investierten Verkaufen
                    _adjustmentProvider.AdjustTradingCandidateSell(currentWorstInvestedCandidate.CurrentWeight, currentWorstInvestedCandidate);
                    _adjustmentProvider.AddToTemporaryPortfolio(currentWorstInvestedCandidate);

                    //neuen kaufen
                    notInvestedCandidate.TransactionType = TransactionType.Open;
                    notInvestedCandidate.TargetWeight = _settings.MaximumInitialPositionSize;
                    _adjustmentProvider.AddToTemporaryPortfolio(notInvestedCandidate);
                    //weiter gehen zum nächsten nicht investierten Kandidaten                                        
                    break;
                }
            }

            //clean up des Cash-Wertes
            CashHandler.CleanUpCash(investedCandidates, investedCandidates);
        }

        private void EnumInvestedCandidates(List<ITradingCandidate> investedCandidates)
        {
            //1. Schauen ob es bereits temporäre transaktionen bei den investierten gibt, dann werden diese bevorzugt
            if (!investedCandidates.Any(x => x.IsTemporary))
                return;
            {
                //Zuerst alle Stops exekutieren
                var stops = investedCandidates.Where(x => x.HasStopp && x.IsBelowStopp);
                foreach (var stop in stops)
                    _adjustmentProvider.AddToTemporaryPortfolio(stop);

                //mach das nur für temporäre mit Aufstockung, sonst kann es sein,
                // dass ich die Enumeration ändere durch den Verkauf (da setzte ich den kandidaten ja auch temporär)
                foreach (var temporaryCandidate in investedCandidates.Where(x => x.IsTemporary && x.HasBetterScoring))
                {
                    //Dann wurde die Position bereits verkauft und ich breake ebenfalls
                    if (_temporaryPortfolio.ContainsCandidate(temporaryCandidate))
                        break;

                    //den Kandidaten entsprechend anpassen
                    _adjustmentProvider.AddToTemporaryPortfolio(temporaryCandidate);

                    //wenn genug cash zur verfügung steht kann ich weitergehen
                    if (!CashHandler.TryHasCash(out var remainingCash))
                    {
                        if (remainingCash > 0)
                            continue;

                        //den aktuell schlechtesten Kandidaten holen
                        var worstCurrentPosition = GetNextCandiateToReplaceFromIdx(investedCandidates);
                        if (worstCurrentPosition == null)
                            break;

                        //Für den Fall, dass alle Positonen gut laufen z.b: sobal ich bei der SecId bin kann ich breaken
                        if (worstCurrentPosition.Record.SecurityId == temporaryCandidate.Record.SecurityId)
                            break;

                        _adjustmentProvider.AdjustTradingCandidateSell(worstCurrentPosition.CurrentWeight, worstCurrentPosition);
                        //sonst den schlechtesten  verkaufen
                        _adjustmentProvider.AddToTemporaryPortfolio(worstCurrentPosition);
                        //den index erhöhen in jedem Fall erhöhen
                        InvestedCandidateIdx++;
                    }
                    else
                        break;
                }
            }
        }
    }

    public class PortfolioManager : PortfolioManagerBase, IAdjustmentProvider, IExposureReceiver
    {
        /// <summary>
        /// Default Constructor for Initializing Settings from Interfaces - Initializes default values if no arguments are passed in
        /// </summary>
        public PortfolioManager(IStopLossSettings stopLossSettings = null, IPortfolioSettings portfolioSettings = null, ITransactionsHandler transactionsHandler = null)
            : base(stopLossSettings ?? new DefaultStopLossSettings(), portfolioSettings ?? new DefaultPortfolioSettings(), transactionsHandler ?? new TransactionsHandler())
        {
            //Initialisierungen
            CashHandler = new CashManager(this);
            TransactionCaclulationProvider = new TransactionCalculationHandler(this);
            TemporaryPortfolio = new TemporaryPortfolio(this);
            CashHandler.Cash = PortfolioSettings.InitialCashValue;
            AllocationToRiskWatcher = new ExposureWatcher(this, PortfolioSettings, IndexType.EuroStoxx50);
            RebalanceProvider = new RebalanceProvider(TemporaryPortfolio, this, PortfolioSettings);

            //Register Events
            PortfolioAsofChangedEvent += OnPortfolioAsOfChanged;
            PositionChangedEvent += OnPositionChanged;
            CashHandler.CashChangedEvent += OnCashChanged;
        }



        public decimal MaximumAllocationToRisk { get; set; } = 1;

        public decimal MinimumAllocationToRisk { get; set; } = 0;

        /// <summary>
        /// der Risk Watcher - kümmert sich um die Berechnung der maximalen Aktienquote
        /// </summary>
        internal IExposureProvider AllocationToRiskWatcher { get; set; }

        /// <summary>
        /// der Rebalance Provider - kümmert sich um das Rebalanced des Portfolios
        /// </summary>
        internal IRebalanceProvider RebalanceProvider { get; set; }


        /// <summary>
        /// das Event das aufgerufen wird, wenn sich die Positonen änder
        /// </summary>
        protected event EventHandler<PortfolioManagerEventArgs> PositionChangedEvent;

        /// <summary>
        /// die Klasse die sich um die Berechnungen der Transaktion kümmert
        /// </summary>
        protected TransactionCalculationHandler TransactionCaclulationProvider { get; }

        /// <summary>
        /// Der CashHandler der sich um die berechnung des Cash-Wertes kümmert
        /// </summary>
        public ICashManager CashHandler { get; }

        //es müsste eine List als temporäres Profolio genügen ich adde alle current Positionen und füge dann die neuen temporär hinzu
        //danach müsste es genügen sie nach scoring zu sortieren und die schlechtesten beginnend abzuschichten, so lange bis ein zulässiger investitionsgrad 
        //erreicht ist
        public ITemporaryPortfolio TemporaryPortfolio { get; }

        /// <summary>
        /// Eine Auflistung die alle Kandidaten enthält die ich zum temporären Portfolio hinzufüge
        /// </summary>
        internal HashSet<ITradingCandidate> TemporaryCandidates { get; } = new HashSet<ITradingCandidate>();

        /// <summary>
        /// Das aktuelle Portfolio (alle Transaktionen die nicht geschlossen sind)
        /// </summary>
        public IEnumerable<ITransaction> CurrentPortfolio => TransactionsHandler.CurrentPortfolio;


        private void OnCashChanged(object sender, DateTime e)
        {
            Trace.TraceInformation($"aktuelles Cash: {CashHandler.Cash:C}");
        }

        /// <summary>
        /// EventCallback wird gefeuert wenn sich das asof Datum erhöht
        /// </summary>
        /// <param name="sender">der Sender (der Pm)</param>
        /// <param name="e">die event args</param>
        /// <param name="dateTime">das asof Datum für TestZwecke</param>
        private void OnPortfolioAsOfChanged(object sender, DateTime dateTime)
        {
            //den aktuellen Portfolio Wert setzen
            CalculateCurrentPortfolioValue();
        }
        /// <summary>
        /// Der EventCallback wenn sich die Transaktionen ändern
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPositionChanged(object sender, PortfolioManagerEventArgs args)
        {
            StopLossSettings.AddOrRemoveDailyLimit(args.Transaction);
        }

        /// <summary>
        /// Hier werden die Candidaten die zur Verfügung stehen injected
        /// </summary>
        /// <param name="candidatesBase"></param>
        /// <param name="asof">das Datum</param>
        public void PassInCandidates([NotNull]List<ITradingCandidateBase> candidatesBase, DateTime asof)
        {
            if (asof >= PortfolioAsof)
            {
                //clearn und neu adden, somit sollte ich immer nur das aktuelle Portfolio zu jedem Bewertungstag haben
                TemporaryPortfolio.Clear();
                TemporaryCandidates.Clear();

                //Wenn bereits Transaktionen getätigt wurden
                if (!TransactionsHandler.CurrentPortfolio.HasItems(asof))
                {
                    //wenn es aktuelle Items gibt füge ich diese in das temporäre Portfolio ein
                    TemporaryPortfolio.AddRange(TransactionsHandler.CurrentPortfolio, false);
                }
            }

            //Den Bewertungszeitpunkt setzen      
            PortfolioAsof = asof;

            var candidates = candidatesBase.Select(x => new TradingCandidate(x, TransactionsHandler, this)).ToList();
            //das bestehenden Portfolio evaluieren und mit dem neuen Score in die Candidatenliste einfügen 
            candidates.AddRange(RankCurrentPortfolio().Select(x => new TradingCandidate(x, TransactionsHandler, this, true)));

            //Nur wenn es bereits ein CurrentPortfolio gibt und an TradingTagen
            if (TransactionsHandler.CurrentPortfolio.IsInitialized && PortfolioAsof.DayOfWeek == PortfolioSettings.TradingDay)
            {
                //überprüfen ob es neue Kandidaten gibt
                foreach (var grp in candidates.GroupBy(x => x.SecurityId))
                {
                    if (grp.Count() <= 1)
                        continue;
                    candidates.Remove(grp.FirstOrDefault(x => !x.IsInvested));
                }
            }

            //Portfolio Regeln anwenden
            ApplyPortfolioRules(candidates.OrderByDescending(x => x.ScoringResult).ToList());
        }

        /// <summary>
        /// Flag das angibt ob es im temporären Portdolio zu Änderungen gekommen ist
        /// </summary>
        public bool HasChanges => TemporaryPortfolio.HasChanges;

        public decimal CurrentAllocationToRisk => 1 - CashHandler.Cash / PortfolioValue;

        protected override void ApplyPortfolioRules(List<TradingCandidate> candidates)
        {
            // Wenn keine Kandidaten vorhanden sind, wir das Portfolio nicht geändert
            if (candidates.Count <= 0)
                return;

            // Wenn keine Aktienquote zulässig ist returne ich ebenfalls
            if (MaximumAllocationToRisk == 0)
                return;

            //liste mit den besten Kandiaten die, aktuelle verdrängen werden
            var bestCandidatesNotInvestedIn = new List<TradingCandidate>();

            //check Candidates
            foreach (var candidate in candidates)
            {
                if (candidate.IsInvested)
                {

                    //und zu dem Zeitpunkt den Score
                    candidate.LastScoringResult = ScoringProvider.GetScore(candidate.Record.SecurityId, candidate.LastTransaction.TransactionDateTime);

                    //es wird nur an Handelstagen aufgestockt
                    if (PortfolioAsof.DayOfWeek == PortfolioSettings.TradingDay && candidate.HasBetterScoring)
                    {
                        //wird nur aufgestockt wenn er lang genug gehalten wurde
                        if (IsBelowMinimumHoldingPeriode(candidate))
                            return;

                        AdjustTradingCandidateBuy(candidate.CurrentWeight, candidate);
                    }
                    //dann ist der aktuelle Score gefallen
                    //hier die Stops checken
                    else if (StopLossSettings.HasStopLoss(candidate))
                    {
                        //wenn die aktuelle Haltedauer kürzer ist als das minimum der Strategie, dann probier ich gegen den nächsten zu tauschen                     
                        if (IsBelowMinimumHoldingPeriode(candidate))
                            continue;

                        if (StopLossSettings.IsBelowMinimumStopHoldingPeriod(candidate))
                            continue;

                        // hier erst das StopFalg setzen
                        candidate.HasStopp = true;
                        //sonst stop realisieren
                        AdjustTradingCandidateSell(candidate.CurrentWeight, candidate);
                    }
                    //weitergehen zum nächsten
                    continue;
                }
                //wenn es kein Handelstag ist, kaufe ich nichts
                if (PortfolioAsof.DayOfWeek != PortfolioSettings.TradingDay)
                    continue;
                //Wenn es kein aktives investment ist würde das System die Positon neu erwerben
                //solange genügen cash dafür vorhanden ist
                if (TryHasCash(out var remainingCash))
                {
                    //Try Open new Position//
                    //den Kadidaten entsprechend flaggen und mit MetaInfos befüllen
                    AdjustTradingCandidateBuy(0, candidate);
                    AddToTemporaryPortfolio(candidate);
                }
                else
                {
                    if (!TransactionsHandler.CurrentPortfolio.IsInitialized)
                        break;
                    //Ich muss mir hier die Positionen merken, die aktuell den höchsten Score haben und aktuell 
                    //nicht im Portfolio sind und auch kein
                    //cash dafür vorhanden ist, dann muss ich das Portfolio ebenso rebuilden => damit die schwächtsen verdrängen
                    //eventuell ein Setting einbauen die zulässt wieviele pro trading tag verdrängt werden ?
                    if (bestCandidatesNotInvestedIn.Count < 10 && remainingCash < PortfolioSettings.MaximumInitialPositionSize * PortfolioValue)
                        bestCandidatesNotInvestedIn.Add(candidate);
                }
            }

            if (bestCandidatesNotInvestedIn.Count > 0)
            {
                if (Debugger.IsAttached && !_initialized)
                {
                    JsonUtils.SerializeToFile(bestCandidatesNotInvestedIn, "BestCandidatesNotInvestedIn.txt");
                    JsonUtils.SerializeToFile(candidates, "Candidates.txt");
                    _initialized = true;
                }
            }

            //das Portfolio Rebalancen
            RebalanceTemporaryPortfolio(bestCandidatesNotInvestedIn, candidates);
          

            //MaxAllocationToRisk in betracht ziehen
            if (MaximumAllocationToRisk == 1)
                return;

            //Todo Implement RiskReduction
            var tempCandidates = TemporaryCandidates.Where(x => x.TransactionType != TransactionType.Close)
                .OrderBy(c => c.ScoringResult.Score).ToList();

            var investedCandidates = candidates.Where(x => x.IsInvested && x.TransactionType == TransactionType.Unknown)
                .OrderBy(x => x.ScoringResult.Score).ToList();


            if (MaximumAllocationToRisk == 0)
            {
                //forcen des Totalverkaufs
                for (var idx = 0; idx < investedCandidates.Count; idx++)
                {
                    var investedCandidate = investedCandidates[idx];
                    if (!investedCandidate.IsTemporary)
                    {
                        AdjustTradingCandidateSell(decimal.Zero, investedCandidate);
                        AddToTemporaryPortfolio(investedCandidate);
                        investedCandidates.Remove(investedCandidate);
                    }
                    else
                    {
                        AdjustTemoraryPosition(investedCandidate);
                        investedCandidates.Remove(investedCandidate);
                    }
                }
            }
            else
            {

                while (CurrentAllocationToRisk > MaximumAllocationToRisk)
                {
                    for (var idx = 0; idx < tempCandidates.Count; idx++)
                    {
                        if (CurrentAllocationToRisk <= MaximumAllocationToRisk)
                            break;

                        var temporaryCandidate = tempCandidates[idx];

                        if (!temporaryCandidate.IsTemporary)
                        {
                            //wenn das Target Weight kleiner ist handelt es sich bereits um eine geplante Abschichtung => dann fliegt die Position komplett raus
                            AdjustTradingCandidateSell(temporaryCandidate.TargetWeight < temporaryCandidate.CurrentWeight
                                ? temporaryCandidate.TargetWeight
                                : temporaryCandidate.CurrentWeight, temporaryCandidate);

                            AddToTemporaryPortfolio(temporaryCandidate);
                        }
                        else
                        {
                            AdjustTemoraryPosition(temporaryCandidate);
                        }
                        //sicherstellen, dass ich die Transaktion nicht 2 mal verkaufe
                        tempCandidates.Remove(temporaryCandidate);
                    }

                    //dann reichen die bestehenden temporären Postion nicht aus,
                    //dann gehe ich die aktuellen Bestände durch
                    for (var idx = 0; idx < investedCandidates.Count; idx++)
                    {
                        if (CurrentAllocationToRisk <= MaximumAllocationToRisk)
                            break;

                        var investedCandidate = investedCandidates[idx];
                        if (!investedCandidate.IsTemporary)
                        {
                            AdjustTradingCandidateSell(investedCandidate.CurrentWeight, investedCandidate);
                            AddToTemporaryPortfolio(investedCandidate);
                        }
                        else
                        {
                            AdjustTemoraryPosition(investedCandidate);
                        }
                        investedCandidates.Remove(investedCandidate);
                    }

                    if (tempCandidates.Count == 0 && investedCandidates.Count == 0)
                        break;
                }
            }

        }

        private bool _initialized;

        private void AdjustTradingCandidateBuy(decimal currentWeight, TradingCandidate candidate)
        {
            //wird nicht mehr aufgestockt bereits am maximum
            //der nächst bessere Candidate wird berücksichtigt
            if (currentWeight > PortfolioSettings.MaximumPositionSize - PortfolioSettings.MaximumPositionSizeBuffer)
                return;

            //meta Info setzen
            candidate.TransactionType = TransactionType.Changed;
            candidate.IsTemporary = true;

            if (currentWeight.IsBetween(decimal.Zero, new decimal(0.08)))
            {
                //wird auf die initial größe zurück aufgestockt
                candidate.TargetWeight = PortfolioSettings.MaximumInitialPositionSize;
                candidate.TransactionType = TransactionType.Open;
            }

            //Position wurde bereits einmal mit Target 10% eröffnet
            else if (currentWeight.IsBetween(new decimal(0.08), new decimal(0.18)))
            {
                //wird nun auf 20% aufgestockt
                candidate.TargetWeight = PortfolioSettings.MaximumInitialPositionSize * 2;
            }

            //schon einaml aufgestockt
            else if (currentWeight.IsBetween(new decimal(0.18), new decimal(0.28)))
            {
                //wird auf den maximal Wert aufgestockt
                candidate.TargetWeight = PortfolioSettings.MaximumPositionSize;
            }
        }

        public void AdjustTradingCandidateSell(decimal currentWeight, ITradingCandidate candidate)
        {
            candidate.IsTemporary = true;
            candidate.TransactionType = TransactionType.Changed;
            //wennn die Position bereits am maximum ist
            if (currentWeight > PortfolioSettings.MaximumPositionSize - PortfolioSettings.MaximumPositionSizeBuffer)
            {
                //in dem Fall um den Factor verringern
                candidate.TargetWeight = (decimal)StopLossSettings.ReductionValue * currentWeight;
            }

            //Position wurde bereits einmal mit Target 10% eröffnet und wird totalverkauft
            else if (currentWeight.IsBetween(decimal.Zero,
                PortfolioSettings.MaximumInitialPositionSize * 2 - PortfolioSettings.MaximumPositionSizeBuffer))
            {
                //AdjustTemporaryPortfolio(0, TransactionType.Close, candidate, true);
                candidate.TargetWeight = decimal.Zero;
                candidate.TransactionType = TransactionType.Close;
            }
            else
            {
                //sonst auf die initale Größe zurückstufen (ist irgendwo > PortfolioSettings.MaximumInitialPositionSize * 2 - PortfolioSettings.MaximumPositionSizeBuffer und < PortfolioSettings.MaximumPositionSize - PortfolioSettings.MaximumPositionSizeBuffer
                candidate.TargetWeight = PortfolioSettings.MaximumInitialPositionSize;
            }
        }

        private void RebalanceTemporaryPortfolio([NotNull] List<TradingCandidate> bestCandidates, [NotNull] List<TradingCandidate> allCandidates)
        {
            //allCandidates.ForEach(x => Trace.TraceInformation(PortfolioAsof.ToShortDateString() + "_" + x.ToString()));
            //bestCandidates.ForEach(x => Trace.TraceInformation(PortfolioAsof.ToShortDateString() + "_" + x.ToString()));
            //investierte Candidaten
            var investedCandidates = allCandidates.Where(x => x.IsInvested).ToList();

            //der aktuelle index in der investedCandidates Liste
            var investedIdx = 1;

            //gibt mir den ncähsten Kandiaten zum Austauschen zurück
            TradingCandidate GetNextInvestedCandiateToReplace()
            {
                if (investedIdx == investedCandidates.Count)
                    return null;

                var worstCurrentPosition = investedCandidates[investedCandidates.Count - investedIdx];
                //Wenn der Kandiat noch nicht im temporären Portfolio ist kann ich ihn zurückgeben
                if (!TemporaryPortfolio.ContainsCandidate(worstCurrentPosition))
                    return worstCurrentPosition;

                //sonst hat es die Position bereits ausgestoppt und ich probiere den nächsten
                investedIdx++;
                return GetNextInvestedCandiateToReplace();
            }

            //1. Schauen ob es bereits temporäre transaktionen bei den investierten gibt, dann werden diese bevorzugt
            if (investedCandidates.Any(x => x.IsTemporary))
            {
                //Zuerst alle Stops exekutieren
                var stops = investedCandidates.Where(x => x.HasStopp && x.IsBelowStopp);
                foreach (var stop in stops)
                    AddToTemporaryPortfolio(stop);

                //mach das nur für temporäre mit Aufstockung, sonst kann es sein,
                // dass ich die Enumeration ändere durch den verkauf (da setzte ich den kandidaten ja auch temporär)
                foreach (var temporaryCandidate in investedCandidates.Where(x => x.IsTemporary && x.HasBetterScoring))
                {
                    //Dann wurde die Position bereits verkauft und ich breake ebenfalls
                    if (TemporaryPortfolio.ContainsCandidate(temporaryCandidate))
                        break;

                    //den Kandidaten entsprechend anpassen
                    AddToTemporaryPortfolio(temporaryCandidate);

                    //wenn genug cash zur verfügung steht kann ich weitergehen
                    if (!CashHandler.TryHasCash(out var remainingCash))
                    {
                        if (remainingCash > 0)
                            continue;

                        //den aktuell schlechtesten Kandidaten holen
                        var worstCurrentPosition = GetNextInvestedCandiateToReplace();
                        if (worstCurrentPosition == null)
                            break;

                        //Für den Fall, dass alle Positonen gut laufen z.b: sobal ich bei der SecId bin kann ich breaken
                        if (worstCurrentPosition.SecurityId == temporaryCandidate.SecurityId)
                            break;

                        AdjustTradingCandidateSell(worstCurrentPosition.CurrentWeight, worstCurrentPosition);
                        //sonst den schlechtesten  verkaufen
                        AddToTemporaryPortfolio(worstCurrentPosition);
                        //den index erhöhen in jedem Fall erhöhen
                        investedIdx++;
                    }
                    else
                        break;
                }
            }

            //dann an dieser Stelle abbrechen
            if (bestCandidates.Count == 0)
                return;

            //2. schauen ob der ersten candidate in der Liste einen besseren Score hat als der aktuelle
            //komme hier nun mit einem ausgegelichen Cash Konto her
            var best = bestCandidates[0];

            if (investedCandidates.Count == 0)
                return;

            var nextCandidate = GetNextInvestedCandiateToReplace();
            if (nextCandidate == null)
            {
                CleanUpCash(investedCandidates, investedCandidates);
                return;
            }

            //wenn der beste Kandidat keinen höheren Score als der aktuell schlechteste hat brauch ich mir die anderen erst gar nicht anzusehen
            if (best.ScoringResult.Score < nextCandidate.ScoringResult.Score * (1 + PortfolioSettings.ReplaceBufferPct))
            {
                CleanUpCash(investedCandidates, investedCandidates);
                return;
            }

            //flag das angibt ob ich aus der foreach breaken kann
            var isbetterCandidateLeft = true;

            //Hier werden nur die Kandidaten ausgetauscht
            foreach (var notInvestedCandidate in bestCandidates)
            {
                //abbreuchbedingung wenn kein besser kandidat mehr in der Liste enthaltebn ist
                if (!isbetterCandidateLeft)
                    break;

                //wenn er bereits im Temporären Portfolio ist zum nächst besseren
                if (notInvestedCandidate.IsTemporary)
                {
                    //dann brauch ich einen neuen Kandiaten zum Abschichten
                    investedIdx++;
                    continue;
                }

                //wenn er berties investiert ist weiter
                if (notInvestedCandidate.IsInvested)
                    continue;

                while (investedIdx < investedCandidates.Count)
                {
                    var currentWorstInvestedCandidate = GetNextInvestedCandiateToReplace();
                    if (currentWorstInvestedCandidate == null)
                        break;
                    investedIdx++;

                    if (currentWorstInvestedCandidate.HasBetterScoring || currentWorstInvestedCandidate.IsTemporary)
                    {
                        investedIdx++;
                        continue;
                    }

                    //wenn der nicht investierte Kandidate schlechter ist als der investierte ignorieren
                    //an diesem Punkt kann ich davon ausgehen, dass auch die nächsten Kandidaten nicht mehr besser sind
                    // und abbrechen
                    if (notInvestedCandidate.ScoringResult.Score <= currentWorstInvestedCandidate.ScoringResult.Score *
                        (1 + PortfolioSettings.ReplaceBufferPct))
                    {
                        isbetterCandidateLeft = false;
                        break;
                    }

                    //wenn die Position bereits aufgestockt wurde tausche ich sie nicht aus
                    if (currentWorstInvestedCandidate.CurrentWeight > new decimal(0.12))
                        continue;

                    //wenn kleiner als die mimimum holding periode weiter
                    if (IsBelowMinimumHoldingPeriode(currentWorstInvestedCandidate))
                        continue;

                    //investierten Verkaufen
                    AdjustTradingCandidateSell(currentWorstInvestedCandidate.CurrentWeight, currentWorstInvestedCandidate);
                    AddToTemporaryPortfolio(currentWorstInvestedCandidate);

                    //neuen kaufen
                    notInvestedCandidate.TransactionType = TransactionType.Open;
                    notInvestedCandidate.TargetWeight = PortfolioSettings.MaximumInitialPositionSize;
                    AddToTemporaryPortfolio(notInvestedCandidate);
                    //weiter gehen zum nächsten nicht investierten Kandidaten                                        
                    break;
                }
            }

            //clean up des Cash-Wertes
            CleanUpCash(investedCandidates, investedCandidates);
        }

        private void CleanUpCash(List<TradingCandidate> allCandidates, List<TradingCandidate> investedCandidates)
        {
            //Hier bin nich mit dem Rebalancing fertig
            if (CashHandler.Cash < 0)
            {
                //die Temporären Kandidaten
                var tempToAdjust = allCandidates.Where(x => x.IsTemporary && x.TransactionType != TransactionType.Close)
                    .ToList();

                //Cash Clean Up der temporären auf Puffer Größe
                if (tempToAdjust.Count > 0)
                {
                    for (var i = tempToAdjust.Count - 1; i >= 0; i--)
                    {
                        var current = tempToAdjust[i];
                        if (AdjustTemporaryPortfolioToCashPuffer(CashHandler.Cash, current, true))
                            break;
                    }
                }
                //cash clean up der investierten
                else
                {
                    var investedToAdjust = investedCandidates.Where(x => !x.IsTemporary).ToList();

                    for (var i = investedToAdjust.Count - 1; i >= 0; i--)
                    {
                        var current = investedToAdjust[i];
                        //sicherheitshalber nochmal checken ob nicht im temporären portfolio
                        if (TemporaryPortfolio.IsTemporary(current.SecurityId))
                            continue;
                        current.TransactionType = TransactionType.Changed;
                        if (AdjustTemporaryPortfolioToCashPuffer(CashHandler.Cash, current))
                            break;
                    }

                    if (CashHandler.Cash < 0)
                    {
                    }
                }
            }
            else if (CashHandler.TryHasCash(out var remainingCash))
            {
                //Dann ist noch Cash für einen Kandiaten über
            }
        }

        public bool IsBelowMinimumHoldingPeriode(ITradingCandidate candidate)
        {
            var lastTransaction = candidate.LastTransaction;
            if (candidate.IsBelowStopp)
            {
                lastTransaction = TransactionsHandler.GetSingle(candidate.Record.SecurityId, TransactionType.Open);
            }
            else
            {
                switch (lastTransaction.TransactionType)
                {
                    case TransactionType.Close:
                    case TransactionType.Open:
                    case TransactionType.Unknown:
                        lastTransaction = TransactionsHandler.GetSingle(candidate.Record.SecurityId, TransactionType.Open);
                        break;
                    case TransactionType.Changed:
                        lastTransaction = TransactionsHandler.GetSingle(candidate.Record.SecurityId, TransactionType.Changed);
                        break;
                }
            }

            if (lastTransaction == null)
                return false;

            //wenn die aktuelle Haltedauer kürzer ist als das minimum der Strategie, dann probier ich gegen den nächsten zu tauschen
            return (PortfolioAsof - lastTransaction.TransactionDateTime).Days < PortfolioSettings.MinimumHoldingPeriodeInDays;
        }

        protected override IEnumerable<ITradingCandidateBase> RankCurrentPortfolio()
        {
            if (ScoringProvider == null)
                throw new MissingMemberException($"Achtung es wurde kein {nameof(ScoringProvider)} übergeben!");

            // das aktuelle Portfolio neu bewerten
            foreach (var transactionItem in CurrentPortfolio)
            {
                //das Result vom Scoring Provider abfragen und neuen Candidaten returnen
                var result = ScoringProvider.GetScore(transactionItem.SecurityId, PortfolioAsof);
                yield return new Candidate(ScoringProvider.GetTradingRecord(transactionItem.SecurityId, PortfolioAsof), result);
            }
        }

        public bool AdjustTemporaryPortfolioToCashPuffer(decimal missingCash, ITradingCandidate candidate, bool adjustPosition = false)
        {
            //TODO: refactoren
            //die aktuelle transaktion // wenn null dann muss ich sie mir aus dem temporären portfolio holen 
            //da ich nicht investiert bin
            var current = candidate.IsTemporary
                ? TemporaryPortfolio.Get(candidate.Record.SecurityId)
                : TransactionsHandler.CurrentPortfolio[candidate.Record.SecurityId];

            //die aktuelle Bewertung der Position
            var currentValue = candidate.IsTemporary ? Math.Abs(current.TargetAmountEur) : Math.Abs(current.Shares) * ScoringProvider.GetTradingRecord(current.SecurityId, PortfolioAsof).AdjustedPrice;

            //das Ziel Cash inklusive Puffer
            var targetCash = PortfolioValue * PortfolioSettings.CashPufferSize;

            //den Ziel-Wert berechnen => wenn der kleiner als null ist geht es sich mit dieser einen Transkation nicht aus daher totalverkaufen           
            var targetValue = currentValue - (Math.Abs(missingCash) + targetCash);

            if (targetValue < 1500)
            {
                //es geht sich nicht aus daher totalverkauf
                candidate.TargetWeight = decimal.Zero;
                candidate.TransactionType = TransactionType.Close;
                if (adjustPosition)
                    AdjustTemoraryPosition(candidate);
                else
                    AddToTemporaryPortfolio(candidate);

                return targetValue >= 0;
            }
            //das neue Target-Gewicht
            var newtargetWeight = Math.Round(targetValue / PortfolioValue, 4);

            //candidate.TargetWeight = adjustPosition
            //    ? Math.Round(candidate.TargetWeight * (targetValue / currentValue), 4)
            //    : newtargetWeight;
            candidate.TargetWeight = newtargetWeight;

            //sonst die Position abschichten
            if (adjustPosition)
                AdjustTemoraryPosition(candidate);
            else
                AddToTemporaryPortfolio(candidate);

            return true;
        }

        private void AdjustTemoraryPosition(ITradingCandidate candidate)
        {
            var targetAmount = TransactionCaclulationProvider.CalculateTargetAmount(candidate);
            var targetShares = TransactionCaclulationProvider.CalculateTargetShares(candidate, targetAmount);
            var effectiveAmountEur = TransactionCaclulationProvider.CalculateEffectiveAmountEur(candidate, targetShares);
            var effectiveWeight = TransactionCaclulationProvider.CalculateEffectiveWeight(effectiveAmountEur);

            if (!TemporaryPortfolio.ContainsCandidate(candidate, false))
                return;

            //die aktuell geplante unveränderte Transaction
            var tempItem = TemporaryPortfolio.Get(candidate.Record.SecurityId);

            //die Region reverted zuerst den Casheffect (rücklbuchung und erstellt dann einen neuen aktualisierten Eintrag)
            using (new CashEffectiveRange(tempItem, TemporaryPortfolio))
            {
                tempItem.TargetWeight = candidate.TargetWeight;
                tempItem.TargetAmountEur = targetAmount;
                tempItem.Shares = targetShares;
                tempItem.EffectiveAmountEur = effectiveAmountEur;
                tempItem.EffectiveWeight = effectiveWeight;
            }
        }



        public void AddToTemporaryPortfolio(ITradingCandidate candidate)
        {
            //zur temporären Liste hinzufügen
            TemporaryCandidates.Add(candidate);

            var targetAmount = TransactionCaclulationProvider.CalculateTargetAmount(candidate);
            var targetShares = TransactionCaclulationProvider.CalculateTargetShares(candidate, targetAmount);
            var effectiveAmountEur = TransactionCaclulationProvider.CalculateEffectiveAmountEur(candidate, targetShares);
            var effectiveWeight = TransactionCaclulationProvider.CalculateEffectiveWeight(effectiveAmountEur);

            Transaction transaction;
            switch (candidate.TransactionType)
            {
                case TransactionType.Open:
                    //transaktion erstellen
                    transaction = CreateTransaction(candidate, targetAmount, targetShares, effectiveAmountEur, effectiveWeight);
                    //Add to temporary Portfolio
                    TemporaryPortfolio.Add(transaction);
                    break;
                case TransactionType.Close:
                    //transaktion erstellen
                    transaction = CreateTransaction(candidate, targetAmount, targetShares, effectiveAmountEur, effectiveWeight);
                    //Add to temporary Portfolio
                    TemporaryPortfolio.Add(transaction);
                    PositionChangedEvent?.Invoke(this, new PortfolioManagerEventArgs(transaction));
                    break;
                case TransactionType.Changed:
                    //transaktion erstellen
                    transaction = CreateTransaction(candidate, targetAmount, targetShares, effectiveAmountEur, effectiveWeight);
                    //Add to temporary Portfolio
                    TemporaryPortfolio.Add(transaction);
                    if (targetShares < 0)
                        PositionChangedEvent?.Invoke(this, new PortfolioManagerEventArgs(transaction));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(candidate.TransactionType), candidate.TransactionType, null);
            }
        }


        private Transaction CreateTransaction(ITradingCandidate candidate, decimal targetAmount, int targetShares, decimal effectiveAmountEur, decimal effectiveWeight)
        {
            return new Transaction
            {
                Shares = targetShares,
                TargetAmountEur = targetAmount,
                TargetWeight = candidate.TargetWeight,
                SecurityId = candidate.Record.SecurityId,
                TransactionDateTime = PortfolioAsof,
                TransactionType = candidate.TransactionType,
                EffectiveWeight = effectiveWeight,
                EffectiveAmountEur = effectiveAmountEur,
                //Name = candidate.Name
            };
        }


        /// <summary>
        /// Registiert den Scoring Provider, der den Score berechnet
        /// </summary>
        /// <param name="scoringProvider"></param>
        public override void RegisterScoringProvider(IScoringProvider scoringProvider)
        {
            ScoringProvider = scoringProvider;
            TransactionsHandler.RegisterScoringProvider(scoringProvider);
        }


        /// <summary>
        /// Berechnet den aktuellen Portfolio-Wert
        /// </summary>
        /// <returns></returns>
        protected override void CalculateCurrentPortfolioValue()
        {
            //deklaration der Summe der investierten Positionen, bewertet mit dem aktuellen Preis
            decimal sumInvested = 0;

            //zur aktuellen Bewertung brauche ich den aktuellen Preis =>
            //average Preis meiner holdings brauch ich nur für Performance
            foreach (var transactionItem in CurrentPortfolio)
            {
                var price = ScoringProvider.GetTradingRecord(transactionItem.SecurityId, PortfolioAsof)?.Price;
                if (price == null)
                    throw new NullReferenceException($"Achtung GetTradingRecord hat zum dem Datum {PortfolioAsof} null zurückgegeben für die SecId {transactionItem.SecurityId}");

                //updaten der Stop Loss Limits
                StopLossSettings.UpdateDailyLimits(transactionItem, price, PortfolioAsof);
                //summe erhöhen
                sumInvested += transactionItem.Shares * price.Value;
            }

            //den Portfolio Wert berechnen
            PortfolioValue = Math.Round(sumInvested, 4) + CashHandler.Cash;

            //die Allokation to Risk berechnen
            AllocationToRisk = sumInvested == 0 ? 1 : Math.Round(sumInvested / PortfolioValue, 4);

            //den aktuellen maximalen Wert berechnen
            AllocationToRiskWatcher.CalculateMaximumExposure(PortfolioAsof);

            //log Value
            //SimpleTextParser.AppendToFile(new List<PortfolioValuation> {new PortfolioValuation(this) },PortfolioSettings.LoggingPath);
        }

        /// <summary>
        /// Gibt true zurück, solange, die Summe der prozentualen Gewichte kleiner als der maximal erlaubte Investitonsgrad ist
        /// und zusätzlich den Wert des zur Veranlagung stehenden Cashs
        /// </summary>
        /// <returns></returns>
        internal bool TryHasCash(out decimal remainingCash)
        {
            return CashHandler.TryHasCash(out remainingCash);
        }
    }

    public class PortfolioManagerEventArgs
    {
        public Transaction Transaction { get; }

        public PortfolioManagerEventArgs(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}