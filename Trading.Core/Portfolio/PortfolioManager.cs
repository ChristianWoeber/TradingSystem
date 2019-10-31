using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Trading.Core.Candidates;
using Trading.Core.Cash;
using Trading.Core.Exposure;
using Trading.Core.Extensions;
using Trading.Core.Rebalancing;
using Trading.Core.Settings;
using Trading.Core.Transactions;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Transaction = Trading.Core.Models.Transaction;

namespace Trading.Core.Portfolio
{
    public class PortfolioManager : PortfolioManagerBase, IPortfolioManager
    {
        /// <summary>
        /// Default Constructor for Initializing Settings from Interfaces - Initializes default values if no arguments are passed in
        /// </summary>
        public PortfolioManager(IStopLossSettings stopLossSettings = null, IPortfolioSettings portfolioSettings = null, ITransactionsHandler transactionsHandler = null)
            : base(stopLossSettings ?? new DefaultStopLossSettings(), portfolioSettings ?? new DefaultPortfolioSettings(), transactionsHandler ?? new TransactionsHandler())
        {
            //Initialisierungen
            CashHandler = new CashManager(this);
            TransactionCaclulationProvider = new TransactionCalculationHandler(this, PortfolioSettings);
            TemporaryPortfolio = new TemporaryPortfolio(this);
            CashHandler.Cash = PortfolioSettings.InitialCashValue;
            AllocationToRiskWatcher = new ExposureWatcher(PortfolioSettings, new FileExposureDataProvider(PortfolioSettings.IndicesDirectory));
            RebalanceProvider = new RebalanceProvider(TemporaryPortfolio, this, PortfolioSettings);
            PositionWatcher = new PositionWatchService(StopLossSettings);

            //Register Events
            PortfolioAsofChangedEvent += OnPortfolioAsOfChanged;
            PositionChangedEvent += OnPositionChanged;
            CashHandler.CashChangedEvent += OnCashChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        public IPositionWatchService PositionWatcher { get; set; }

        /// <summary>
        /// der Risk Watcher - kümmert sich um die Berechnung der maximalen Aktienquote
        /// </summary>
        public IExposureProvider AllocationToRiskWatcher { get; set; }

        /// <summary>
        /// der Rebalance Provider - kümmert sich um das Rebalanced des Portfolios
        /// </summary>
        public IRebalanceProvider RebalanceProvider { get; set; }

        /// <summary>
        /// das Event das aufgerufen wird, wenn eine position ausgestoppt wurde
        /// </summary>
        public event EventHandler<PortfolioManagerEventArgs> StoppLossExecuted;

        /// <summary>
        /// das Event das aufgerufen wird, wenn sich die Positonen änder
        /// </summary>
        public event EventHandler<PortfolioManagerEventArgs> PositionChangedEvent;

        /// <summary>
        /// die Klasse die sich um die Berechnungen der Transaktion kümmert, Shares, Amount EUR etc..
        /// </summary>
        public TransactionCalculationHandler TransactionCaclulationProvider { get; }

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
        public Dictionary<int, ITradingCandidate> TemporaryCandidates { get; } = new Dictionary<int, ITradingCandidate>();

        /// <summary>
        /// Das aktuelle Portfolio (alle Transaktionen die nicht geschlossen sind)
        /// </summary>
        public IEnumerable<ITransaction> CurrentPortfolio => TransactionsHandler.CurrentPortfolio;

        /// <summary>
        /// Das erlaubte minimum inklusive Puffer
        /// </summary>
        public decimal MinimumBoundary => PortfolioSettings.MaximumAllocationToRisk - PortfolioSettings.AllocationToRiskBuffer < 0
            ? 0
            : PortfolioSettings.MaximumAllocationToRisk - PortfolioSettings.AllocationToRiskBuffer;

        /// <summary>
        /// Das erlaubte Maximum inklusive Puffer
        /// </summary>
        public decimal MaximumBoundary => PortfolioSettings.MaximumAllocationToRisk == 1
            ? 1
            : PortfolioSettings.MaximumAllocationToRisk + PortfolioSettings.AllocationToRiskBuffer;


        /// <summary>
        /// die Aktuelle Auslastung
        /// </summary>
        public decimal CurrentSumInvestedEffectiveWeight
        {
            get
            {
                decimal sum = 0;
                //gehe alle nicht gecancelten temporären Transaktionen durch
                foreach (var grp in TemporaryPortfolio.Where(x => x.Cancelled != 1).GroupBy(x => x.SecurityId))
                {
                    if (grp.Any(x => x.TransactionType == TransactionType.Close))
                        continue;

                    foreach (var temporaryTransaction in grp)
                    {
                        if (temporaryTransaction.TransactionDateTime < PortfolioAsof)
                        {
                            sum += temporaryTransaction.Shares * ScoringProvider
                                       .GetTradingRecord(temporaryTransaction.SecurityId, PortfolioAsof).AdjustedPrice;
                        }
                        else
                        {
                            sum += temporaryTransaction.EffectiveAmountEur;
                        }
                    }
                }

                return sum / PortfolioValue;
            }
        }


        private void OnCashChanged(object sender, DateTime e)
        {
            // Trace.TraceInformation($"aktuelles Cash: {CashHandler.Cash:C}");
        }

        /// <summary>
        /// EventCallback wird gefeuert wenn sich das asof Datum erhöht
        /// </summary>
        /// <param name="sender">der Sender (der Pm)</param>
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
            ApplyPortfolioRules(candidates.OrderByDescending(x => x.ScoringResult).Cast<ITradingCandidate>().ToList());
        }


        /// <summary>
        /// Flag das angibt ob es im temporären Portdolio zu Änderungen gekommen ist
        /// </summary>
        public bool HasChanges => TemporaryPortfolio.HasChanges;

        [Obsolete("Redundant sollte ja mit Allocation to Risk übereinstimmen")]
        public decimal CurrentAllocationToRisk => 1 - CashHandler.Cash / PortfolioValue;

        public override void ApplyPortfolioRules(List<ITradingCandidate> candidates)
        {
            // Wenn keine Kandidaten vorhanden sind, wir das Portfolio nicht geändert
            if (candidates.Count <= 0)
                return;

            // Wenn keine Aktienquote zulässig ist returne ich ebenfalls
            if (AllocationToRisk <= 0 && PortfolioSettings.MaximumAllocationToRisk <= 0)
                return;

            //liste mit den besten Kandiaten die, aktuelle verdrängen werden
            var bestCandidatesNotInvestedIn = new List<ITradingCandidate>();

            //check Candidates
            foreach (var candidate in candidates)
            {
                if (candidate.IsInvested)
                {
                    //und zu dem Zeitpunkt den Score
                    candidate.LastScoringResult = ScoringProvider.GetScore(candidate.Record.SecurityId, candidate.LastTransaction.TransactionDateTime);
                    //den Kandidat hier defaultmäßig auf  unchanged setzen
                    candidate.TransactionType = TransactionType.Unchanged;

                    //es wird nur an Handelstagen aufgestockt
                    if (PortfolioAsof.DayOfWeek == PortfolioSettings.TradingDay && candidate.IncrementationStrategyProvider.IsAllowedToBeIncremented())
                    {
                        //wird nur aufgestockt wenn er lang genug gehalten wurde
                        if (IsBelowMinimumHoldingPeriode(candidate))
                            continue;

                        AdjustTradingCandidateBuy(candidate.CurrentWeight, candidate);
                    }
                    //dann ist der aktuelle Score gefallen
                    //hier die Stops checken
                    else if (PositionWatcher.HasStopLoss(candidate))
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
                if (TryHasCash(out _))
                {
                    //Nur kaufen wenn zulässig
                    if (CurrentSumInvestedEffectiveWeight >= MaximumBoundary)
                        break;

                    //Try Open new Position//
                    //den Kadidaten entsprechend flaggen und mit MetaInfos befüllen
                    AdjustTradingCandidateBuy(0, candidate);
                }
                else
                {
                    if (!TransactionsHandler.CurrentPortfolio.IsInitialized)
                        break;
                    //Ich muss mir hier die Positionen merken, die aktuell den höchsten Score haben und aktuell 
                    //nicht im Portfolio sind und auch kein
                    //cash dafür vorhanden ist, dann muss ich das Portfolio ebenso rebuilden => damit die schwächtsen verdrängen
                    //eventuell ein Setting einbauen die zulässt wieviele pro trading tag verdrängt werden ?
                    if (bestCandidatesNotInvestedIn.Count < 10)
                    {
                        //bevor ich die Kandidaten einfüge setzte ich noch die notwendigen Properties für einen initial Buy
                        AdjustTradingCandidateBuy(0, candidate);
                        bestCandidatesNotInvestedIn.Add(candidate);
                    }
                }
            }

            //das Portfolio Rebalancen
            // RebalanceTemporaryPortfolio(bestCandidatesNotInvestedIn, candidates);
            RebalanceProvider.RebalanceTemporaryPortfolio(bestCandidatesNotInvestedIn.Select(c => c).ToList(), candidates.Select(c => c).ToList());
        }

        public void AdjustTradingCandidateBuy(decimal currentWeight, ITradingCandidate candidate)
        {
            //wird nicht mehr aufgestockt bereits am maximum
            //der nächst bessere Candidate wird berücksichtigt
            if (currentWeight > PortfolioSettings.MaximumPositionSize - PortfolioSettings.MaximumPositionSizeBuffer)
                return;

            //meta Info setzen
            candidate.TransactionType = TransactionType.Changed;

            if (currentWeight.IsBetween(decimal.Zero, PortfolioSettings.MaximumInitialPositionSize - PortfolioSettings.MinimumPositionSizePercent))
            {
                //wird auf die initial größe zurück aufgestockt
                candidate.TargetWeight = PortfolioSettings.MaximumInitialPositionSize;
                candidate.TransactionType = TransactionType.Open;
            }

            //Position wurde bereits einmal mit Target 10% eröffnet
            else if (currentWeight.IsBetween(PortfolioSettings.MaximumInitialPositionSize - PortfolioSettings.MinimumPositionSizePercent, PortfolioSettings.MaximumInitialPositionSize * 2 - PortfolioSettings.MinimumPositionSizePercent))
            {
                //wird nun auf 20% aufgestockt
                candidate.TargetWeight = PortfolioSettings.MaximumInitialPositionSize * 2;
            }

            //schon einaml aufgestockt
            else
            {
                //wird auf den maximal Wert aufgestockt
                candidate.TargetWeight = PortfolioSettings.MaximumPositionSize;
            }
        }


        public void AdjustTradingCandidateSell(decimal currentWeight, ITradingCandidate candidate)
        {
            //candidate.IsTemporary = true;
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
                        break;
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

        public bool AdjustTemporaryPortfolioToRiskBoundary(decimal missingPercent, ITradingCandidate candidate)
        {
            //wenn der kandidat nicht im temporären portfolio ist, ist das Target weight nur das ergebnis des Scorings,
            //sprich das wäre das target weight bei ausrechend cash und nicht besseren Kandidaten!!!
            var weightToAdjust = !candidate.IsTemporary ? candidate.CurrentWeight : candidate.TargetWeight;
            candidate.TargetWeight = weightToAdjust;

            //dann geht sich das Rebalancen nicht mit diesem Kandidat aus und ich totalverkaufe diesen
            if (missingPercent > candidate.TargetWeight)
                return AdjustToClosePosition(candidate);

            //dann den aktuellen Kandidaten anpassen, bzw verringern
            candidate.TargetWeight -= Math.Abs(missingPercent);
            if (candidate.TargetWeight < PortfolioSettings.MinimumPositionSizePercent)
            {
                //wenn das neue Target Weight unter dem minimum liegt mache ich einen Totalverkauf aus der anpssung
                return AdjustToClosePosition(candidate, true);
            }

            //sonst die Anpassung übernehmen
            if (candidate.TransactionType != TransactionType.Open && candidate.TransactionType != TransactionType.Close)
                candidate.TransactionType = TransactionType.Changed;
            //wenn es sich um eine bestehende Postion handelt dann einen neuen Verkauf planen sonst bestehnden Position manipulieren
            if (!candidate.IsTemporary)
                AddToTemporaryPortfolio(candidate);
            else
                AdjustTemoraryPosition(candidate);
            return true;
        }

        private bool AdjustToClosePosition(ITradingCandidate candidate, bool isSufficient = false)
        {
            candidate.TargetWeight = decimal.Zero;
            candidate.TransactionType = TransactionType.Close;

            //Wenn der Kandidat noch nicht im temporären portfolio ist handelt es sich um einen alten Bestand
            //dann muss ich eine neue Transaktion erstellen
            //ansonsten kann ich den bestehden anpassen
            if (!candidate.IsTemporary && candidate.IsInvested)
                AddToTemporaryPortfolio(candidate);
            else
                AdjustTemoraryPosition(candidate);

            return isSufficient;
        }


        //TODO: refactoren
        public bool AdjustTemporaryPortfolioToCashPuffer(decimal missingCash, ITradingCandidate candidate, bool adjustPosition = false)
        {
            //die aktuelle Bewertung der Position, wenn ich investiert bin habe ich das aktuelle Gerwicht schon im Candidaten und brauche nur mit dem PortfolioValue zu multiplizieren
            var currentValue = candidate.IsInvested
                ? Math.Round(candidate.CurrentWeight * PortfolioValue, 4)
                : Math.Round(Math.Abs(candidate.TargetWeight * PortfolioValue)/* * candidate.Record.AdjustedPrice*/, 4);
            var targetValue = candidate.TargetWeight * PortfolioValue;
            var remainingValue = currentValue;

            if (candidate.IsTemporarySell)
                remainingValue = currentValue - targetValue;
            else
                remainingValue = currentValue + targetValue;

            //das Ziel Cash inklusive Puffer
            var targetCash = PortfolioValue * PortfolioSettings.CashPufferSizePercent;

            //den Ziel-Wert berechnen => wenn der kleiner als null ist geht es sich mit dieser einen Transkation nicht aus daher totalverkaufen           
            var targetCashValue = remainingValue - (Math.Abs(missingCash) + targetCash);

            //dann geht es sich nicht aus daher totalverkauf
            if (targetCashValue < 1500)
            {
                candidate.TargetWeight = decimal.Zero;
                candidate.TransactionType = TransactionType.Close;
                if (adjustPosition)
                    AdjustTemoraryPosition(candidate);
                else
                    AddToTemporaryPortfolio(candidate);

                return targetCashValue >= 0;
            }
            //das neue Target-Gewicht
            var newtargetWeight = Math.Round(targetCashValue / PortfolioValue, 4);

            //candidate.TargetWeight = adjustPosition
            //    ? Math.Round(candidate.TargetWeight * (targetValue / currentValue), 4)
            //    : newtargetWeight;
            candidate.TargetWeight = newtargetWeight;
            if (candidate.TransactionType == TransactionType.Unknown || candidate.TransactionType == TransactionType.Unchanged)
            {
                candidate.TransactionType = TransactionType.Changed;
            }
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

            if (candidate.TransactionType == TransactionType.Close && !candidate.IsInvested)
            {
                //dann kann ich die transaktion komplett rauslöschen aus den temporären, bzw. stornieren
                TemporaryPortfolio.CancelCandidate(candidate);
                return;
            }

            //die aktuell geplante unveränderte Transaction
            var tempItem = TemporaryPortfolio.Get(candidate.Record.SecurityId);

            //die Region reverted zuerst den Casheffect (rückbuchung und erstellt dann einen neuen aktualisierten Eintrag)
            using (new CashEffectiveRange(tempItem, TemporaryPortfolio))
            {
                tempItem.TargetWeight = candidate.TargetWeight;
                tempItem.TargetAmountEur = targetAmount;
                tempItem.Shares = targetShares;
                tempItem.EffectiveAmountEur = effectiveAmountEur;
                tempItem.EffectiveWeight = effectiveWeight;
                //ganz wichtig auch den TransaktionsTypen ändern!!
                tempItem.TransactionType = candidate.TransactionType;
            }
        }


        public void AddToTemporaryPortfolio(ITradingCandidate candidate)
        {
            //zur temporären Liste hinzufügen
            TemporaryCandidates.Add(candidate.Record.SecurityId, candidate);
            candidate.IsTemporary = true;

            var targetAmount = TransactionCaclulationProvider.CalculateTargetAmount(candidate);
            var targetShares = TransactionCaclulationProvider.CalculateTargetShares(candidate, targetAmount);
            var effectiveAmountEur = TransactionCaclulationProvider.CalculateEffectiveAmountEur(candidate, targetShares);
            var effectiveWeight = TransactionCaclulationProvider.CalculateEffectiveWeight(effectiveAmountEur);

            var hasStopp = candidate.HasStopp && candidate.IsBelowStopp;

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
                    PositionWatcher.AddOrRemoveDailyLimit(transaction);
                    PositionChangedEvent?.Invoke(this, new PortfolioManagerEventArgs(transaction));
                    if (hasStopp)
                        StoppLossExecuted?.Invoke(this, new PortfolioManagerEventArgs(transaction));
                    break;
                case TransactionType.Changed:
                    //transaktion erstellen
                    transaction = CreateTransaction(candidate, targetAmount, targetShares, effectiveAmountEur, effectiveWeight);
                    //Add to temporary Portfolio
                    TemporaryPortfolio.Add(transaction);
                    PositionChangedEvent?.Invoke(this, new PortfolioManagerEventArgs(transaction));
                    if (hasStopp)
                        StoppLossExecuted?.Invoke(this, new PortfolioManagerEventArgs(transaction));
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
                TicketFee = PortfolioSettings.ExpectedTicketFee
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
        public override void CalculateCurrentPortfolioValue()
        {
            //deklaration der Summe der investierten Positionen, bewertet mit dem aktuellen Preis
            decimal sumInvested = 0;

            //zur aktuellen Bewertung brauche ich den aktuellen Preis =>
            //average Preis meiner holdings brauch ich nur für Performance
            foreach (var currentPosition in CurrentPortfolio)
            {
                var price = ScoringProvider.GetTradingRecord(currentPosition.SecurityId, PortfolioAsof)?.Price;
                if (price == null)
                    throw new NullReferenceException($"Achtung GetTradingRecord hat zum dem Datum {PortfolioAsof} null zurückgegeben für die SecId {currentPosition.SecurityId}");

                //updaten der Stop Loss Limits
                //StopLossSettings.UpdateDailyLimits(currentPosition, price, PortfolioAsof);
                PositionWatcher.UpdateDailyLimits(currentPosition, price, PortfolioAsof);

                //updated das performance Dictionary
                // PositionWatcher.UpdatePerformance(currentPosition, price, PortfolioAsof);

                //summe erhöhen
                sumInvested += currentPosition.Shares * price.Value;
            }

            //Sortiere hier einmalig für jedes Datum das Performance Dictionay
            PositionWatcher.CreateSortedPerformanceDictionary();

            //den Portfolio Wert berechnen
            PortfolioValue = Math.Round(sumInvested, 4) + CashHandler.Cash;

            //die Allokation to Risk berechnen
            AllocationToRisk = sumInvested == 0 ? 0 : Math.Round(sumInvested / PortfolioValue, 4);

            //den aktuellen maximalen Wert berechnen
            AllocationToRiskWatcher.CalculateMaximumExposure(PortfolioAsof);

            if (PortfolioAsof == new DateTime(2016, 12, 08))
            {

            }

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

        public void CalcStartingAllocationToRisk(DateTime start)
        {
            var temp = start.AddYears(-5);
            while (temp < start)
            {
                AllocationToRiskWatcher.CalculateMaximumExposure(temp);
                temp = temp.AddDays(1);
            }
        }

        /// <summary>
        /// die Methode soll alle bestehenden Positionen schließen
        /// </summary>
        public void CloseAllPositions()
        {
            foreach (var candidate in RankCurrentPortfolio().Select(x => new TradingCandidate(x, TransactionsHandler, this, true)))
            {
                if (TemporaryCandidates.ContainsKey(candidate.SecurityId))
                    continue;

                AdjustToClosePosition(candidate);
            }

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