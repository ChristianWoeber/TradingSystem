using HelperLibrary.Database.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using HelperLibrary.Extensions;
using HelperLibrary.Interfaces;
using HelperLibrary.Enums;
using JetBrains.Annotations;

namespace HelperLibrary.Trading.PortfolioManager
{
    // class that holds the portfolio logic
    public class PortfolioManager : PortfolioManagerBase, IAdjustmentProvider
    {
        /// <summary>
        /// Default Constructor for Initializing Settings from Interfaces - Initializes default values if no arguments are passed in
        /// </summary>
        public PortfolioManager(IStopLossSettings stopLossSettings = null, IPortfolioSettings portfolioSettings = null, ITransactionsHandler transactionsHandler = null)
            : base(stopLossSettings ?? new DefaultStopLossSettings(), portfolioSettings ?? new DefaultPortfolioSettings(), transactionsHandler ?? new TransactionsWrapper())
        {
            //Initialisierungen
            CashHandler = new CashManager(this);
            TemporaryPortfolio = new TemporaryPortfolio(PortfolioSettings, this);
            CashHandler.Cash = PortfolioSettings.InitialCashValue;

            //Register Events
            PortfolioAsofChanged += OnPortfolioAsOfChanged;
            PositionChangedEvent += OnPositionChanged;

        }


        protected event EventHandler<PortfolioManagerEventArgs> PositionChangedEvent;

        /// <summary>
        /// Der CashHandler der sich um die berechnung des Cash-Wertes kümmert
        /// </summary>
        public ICashManager CashHandler { get; }


        //es müsste eine List als temporäres Profolio genügen ich adde alle current Positionen und füge dann die neuen temporär hinzu
        //danach müsste es genügen sie nach scoring zu sortieren und die schlechtesten beginnend abzuschichten, so lange bis ein zulässiger investitionsgrad 
        //erreicht ist

        public readonly ITemporaryPortfolio TemporaryPortfolio;

        /// <summary>
        /// Das aktuelle Portfolio (alle Transaktionen die nicht geschlossen sind)
        /// </summary>
        public IEnumerable<TransactionItem> CurrentPortfolio => TransactionsHandler.CurrentPortfolio;


        /// <summary>
        /// EventCallback wird gefeuert wenn sich das asof Datum erhöht
        /// </summary>
        /// <param name="sender">der Sender (der Pm)</param>
        /// <param name="e">die event args</param>
        private void OnPortfolioAsOfChanged(object sender, EventArgs e)
        {
            //den aktuellen Portfolio Wert setzen
            CalculateCurrentPortfolioValue();
            if (Debugger.IsAttached)
                Trace.TraceInformation($"Portfolio-Wert: {PortfolioValue:N}");

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
        /// <param name="candidates">die candidaten fürs trading</param>
        /// <param name="asof">das Datum</param>
        public void PassInCandidates([NotNull]List<TradingCandidate> candidates, DateTime asof)
        {
            if (asof >= PortfolioAsof)
            {
                //clearn und neu adden, somit sollte ich immer nur das aktuelle Portfolio zu jedem Bewertungstag haben
                TemporaryPortfolio.Clear();

                //Wenn bereits Transaktionen getätigt wurden
                if (!TransactionsHandler.CurrentPortfolio.HasItems(asof))
                {
                    //wenn es aktuelle Items gibt füge ich diese in das temporäre Portfolio ein
                    TemporaryPortfolio.AddRange(TransactionsHandler.CurrentPortfolio, false);
                }
            }

            //Den Bewertungszeitpunkt setzen      
            PortfolioAsof = asof;

            //wenn von aussen keine neuen Kandidaten kommen, kann ich einfach die bestehenden investments neu berechnen
            //das bestehenden Portfolio evaluieren und mit dem neuen Score in die Candidatenliste einfügen 

            candidates.AddRange(RankCurrentPortfolio());

            //Nur wenn es bereits ein CurrentPortfolio gibt
            if (TransactionsHandler.CurrentPortfolio.IsInitialized)
            {
                //überprüfen ob es neue Kandidaten gibt
                foreach (var grp in candidates.GroupBy(x => x.Record.SecurityId))
                {
                    if (grp.Count() <= 1)
                        continue;

                    candidates.Remove(grp.First());
                }
            }

            //Portfolio Regeln anwenden
            ApplyPortfolioRules(candidates.OrderByDescending(x => x.ScoringResult).ToList());
        }

        /// <summary>
        /// Flag das angibt ob es im temporären Portdolio zu Änderungen gekommen ist
        /// </summary>
        public bool HasChanges => TemporaryPortfolio.HasChanges;

        protected override void ApplyPortfolioRules(List<TradingCandidate> candidates)
        {
            // Wenn keine Kandidaten vorhanden sind, wir das Portfolio nicht geändert
            if (candidates.Count <= 0)
                return;
            //check Candidates
            foreach (var candidate in candidates)
            {
                // if Position already exists
                if (TransactionsHandler.IsActiveInvestment(candidate.Record.SecurityId) == true)
                {
                    var currentWeight = TransactionsHandler.GetWeight(candidate.Record.SecurityId);
                    if (currentWeight == null)
                        throw new ArgumentException("Achtung die Security mit Id: " + candidate.Record.SecurityId +
                                                    " hat keinen gültigen Wert für ihr Gewicht!");

                    //immer die letzte Transaktion holen
                    var lastTransaction = TransactionsHandler.GetSingle(candidate.Record.SecurityId, null);

                    //und zu dem Zeitpunkt den Score
                    var lastScore = ScoringProvider.GetScore(candidate.Record.SecurityId, lastTransaction.TransactionDateTime);

                    //den durchschnittlichen Preis holen
                    var averagePrice = TransactionsHandler.GetAveragePrice(candidate.Record.SecurityId, PortfolioAsof);

                    //es wird nur an Handelstagen aufgestockt
                    //HINT: Achtung ich ergleich immer mit dem original score... sollte ich besser weiterschleifen oder über den Preis machen??
                    //PerformanceHandler ?
                    if (candidate.ScoringResult.Score > lastScore.Score && PortfolioAsof.DayOfWeek == PortfolioSettings.TradingDay)
                    {
                        //Position wurde bereits einmal mit Target 10% eröffnet
                        if (currentWeight.Value.IsBetween(new decimal(0.01), new decimal(0.18)))
                        {
                            //wird nun auf 20% aufgestockt
                            AdjustTemporaryPortfolio(PortfolioSettings.MaximumInitialPositionSize * 2, TransactionType.Changed, candidate, true);
                            continue;
                        }
                        //schon einaml aufgestockt
                        if (currentWeight.Value.IsBetween(new decimal(0.18), new decimal(0.28)))
                        {
                            //wird auf den maximal Wert aufgestockt
                            AdjustTemporaryPortfolio(PortfolioSettings.MaximumPositionSize, TransactionType.Changed, candidate, true);
                            continue;
                        }
                        //maximum
                        if (currentWeight > new decimal(0.28))
                        {
                            //wird nicht mehr aufgestockt
                            //der nächst bessere Candidate wird berücksichtigt
                            continue;
                        }
                    }
                    //dann ist der aktuelle Score gefallen
                    //hier die Stops checken
                    else if (StopLossSettings.HasStopLoss(candidate))
                    {
                        //Position wurde bereits einmal mit Target 10% eröffnet und wird totalverkauft
                        if (currentWeight.Value.IsBetween(new decimal(0.01), new decimal(0.18)))
                        {
                            AdjustTemporaryPortfolio(0, TransactionType.Close, candidate, true);
                            continue;
                        }

                        if (currentWeight.Value.IsBetween(new decimal(0.18), new decimal(0.28)))
                        {
                            //wird auf den maximal Wert aufgestockt
                            AdjustTemporaryPortfolio(PortfolioSettings.MaximumInitialPositionSize, TransactionType.Changed, candidate, true);
                            continue;
                        }

                        AdjustTemporaryPortfolio((decimal)StopLossSettings.ReductionValue * currentWeight.Value, TransactionType.Changed, candidate, true);
                        continue;
                    }
                }

                //Wenn es kein aktives investment ist würde das System die Positon neu erwerben
                //solange genügen cash dafür vorhanden ist
                if (TryHasCash(out var remainingCash))
                {
                    //Try Open new Position//
                    AdjustTemporaryPortfolio(PortfolioSettings.MaximumInitialPositionSize, TransactionType.Open, candidate);
                }
                else
                {
                    // Wenn der Wert für das ramining Cash >=0 ist veranlage ich sonst break ich aus der foreach
                    if (remainingCash < PortfolioSettings.MaximumInitialPositionSize * PortfolioValue)
                        break;

                    // AdjustTemporaryPortfolio(PortfolioSettings.MaximumInitialPositionSize, TransactionType.Open, candidate);
                }
            }
            //Das Portfolio rebuilden
            TemporaryPortfolio.RebuildPortfolio(ScoringProvider, PortfolioAsof);
        }


        protected override IEnumerable<TradingCandidate> RankCurrentPortfolio()
        {
            if (ScoringProvider == null)
                throw new MissingMemberException($"Achtung es wurde kein {nameof(ScoringProvider)} übergeben!");

            // das aktuelle Portfolio neu bewerten
            foreach (var transactionItem in CurrentPortfolio)
            {
                //das Result vom Scoring Provider abfragen und neuen Candidaten returnen
                var result = ScoringProvider.GetScore(transactionItem.SecurityId, PortfolioAsof);
                yield return new TradingCandidate(ScoringProvider.GetTradingRecord(transactionItem.SecurityId, PortfolioAsof), result);
            }
        }

        public bool AdjustTemporaryPortfolioToCashPuffer(decimal missingCash, TransactionType type, TradingCandidate candidate)
        {
            //die aktuelle transaktion
            var current = TransactionsHandler.CurrentPortfolio[candidate.Record.SecurityId];

            //die aktuelle Bewertung der Position
            var currentValue = current.Shares *
                               ScoringProvider.GetTradingRecord(current.SecurityId, PortfolioAsof).AdjustedPrice;

            //das Ziel Cash inklusive Puffer
            var targetCash = PortfolioValue * PortfolioSettings.CashPufferSize;

            //den Ziel-Wert berechnen => wenn der kleiner als null ist geht es sich mit dieser einen Transkation nicht aus daher totalverkaufen
            decimal targetValue;
            if (missingCash < 0)
                targetValue = currentValue - (Math.Abs(missingCash) + targetCash);
            else
                targetValue = currentValue - (missingCash + targetCash);

            if (targetValue < 0)
            {
                //es geht sich nicht aus daher totalverkauf
                AdjustTemporaryPortfolio(0, TransactionType.Close, candidate, true);
                return false;
            }
            //das neue Target-Gewicht
            var targetPortfolioValue = Math.Round(targetValue / PortfolioValue, 4);

            //sonst die Position abschichten
            AdjustTemporaryPortfolio(targetPortfolioValue, type, candidate, true);
            return true;
        }


        public void AdjustTemporaryPortfolio(decimal targetWeight, TransactionType type, TradingCandidate candidate, bool isInvested = false)
        {
            TransactionItem current = null;
            if (isInvested)
                current = TransactionsHandler.CurrentPortfolio[candidate.Record.SecurityId];

            //der ziel Betrag in EuR
            var targetAmount = Math.Round(PortfolioValue * targetWeight, 4);

            //die ziel shares bestimmen, werden immer abgerundet
            var targetShares = (int)Math.Round(targetAmount / candidate.Record.AdjustedPrice, 2, MidpointRounding.AwayFromZero);

            //wenn ich bereits investiert bin, ziehe ich die bestehenden shares ab
            if (current != null)
            {
                targetShares = targetShares - current.Shares;
                targetAmount = targetAmount - current.TargetAmountEur;
            }

            //das effektive gewicht
            var effectiveAmountEur = Math.Round(targetShares * candidate.Record.AdjustedPrice, 4);

            //wenn es sich um ein Closing einer Position handelt
            if (targetWeight <= 0)
            {
                targetShares = TransactionsHandler.CurrentPortfolio[candidate.Record.SecurityId].Shares * -1;
                targetAmount = TransactionsHandler.CurrentPortfolio[candidate.Record.SecurityId].TargetAmountEur * -1;
                effectiveAmountEur = Math.Round(targetShares * candidate.Record.AdjustedPrice, 4);
            }

            //das effektive Gewicht berechnen
            var effectiveWeight = Math.Round(effectiveAmountEur / PortfolioValue, 4);

            TransactionItem transaction = null;
            switch (type)
            {
                case TransactionType.Open:
                    //transaktion erstellen
                    transaction = new TransactionItem
                    {
                        Shares = targetShares,
                        TargetAmountEur = targetAmount,
                        TargetWeight = targetWeight,
                        SecurityId = candidate.Record.SecurityId,
                        TransactionDateTime = PortfolioAsof,
                        TransactionType = (int)type,
                        EffectiveWeight = effectiveWeight,
                        EffectiveAmountEur = effectiveAmountEur
                    };

                    //Add to temporary Portfolio
                    TemporaryPortfolio.Add(transaction);
                    break;
                case TransactionType.Close:
                    //transaktion erstellen

                    transaction = new TransactionItem
                    {
                        Shares = targetShares,
                        TargetAmountEur = targetAmount,
                        TargetWeight = targetWeight,
                        SecurityId = candidate.Record.SecurityId,
                        TransactionDateTime = PortfolioAsof,
                        TransactionType = (int)type,
                        EffectiveWeight = effectiveWeight,
                        EffectiveAmountEur = effectiveAmountEur
                    };

                    //Add to temporary Portfolio
                    TemporaryPortfolio.Add(transaction);
                    PositionChangedEvent?.Invoke(this, new PortfolioManagerEventArgs(transaction));
                    break;
                case TransactionType.Changed:

                    //transaktion erstellen
                    transaction = new TransactionItem
                    {
                        Shares = targetShares,
                        TargetAmountEur = targetAmount,
                        TargetWeight = targetWeight,
                        SecurityId = candidate.Record.SecurityId,
                        TransactionDateTime = PortfolioAsof,
                        TransactionType = (int)type,
                        EffectiveWeight = effectiveWeight,
                        EffectiveAmountEur = effectiveAmountEur
                    };

                    //Add to temporary Portfolio
                    TemporaryPortfolio.Add(transaction);
                    if (targetShares < 0)
                        PositionChangedEvent?.Invoke(this, new PortfolioManagerEventArgs(transaction));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
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

                StopLossSettings.UpdateDailyLimits(transactionItem, price, PortfolioAsof);

                sumInvested += transactionItem.Shares * price.Value;
            }

            //den Portfolio Wert berechnen
            PortfolioValue = Math.Round(sumInvested, 4) + CashHandler.Cash;

            //die Allokation to Risk berechnen
            AllocationToRisk = Math.Round(sumInvested, 4) / PortfolioValue;
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
        public TransactionItem Transaction { get; }

        public PortfolioManagerEventArgs(TransactionItem transaction)
        {
            Transaction = transaction;
        }
    }
}