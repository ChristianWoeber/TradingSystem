using HelperLibrary.Database.Models;
using HelperLibrary.Enums;
using HelperLibrary.Interfaces;
using System;
using System.Collections.Generic;
using HelperLibrary.Calculations;

namespace HelperLibrary.Trading.PortfolioManager
{
    public class DefaultSaveProvider : ISaveProvider
    {
        public void Save(IEnumerable<Transaction> items)
        {
            throw new NotImplementedException();
        }
    }


    public class ConservativePortfolioSettings : IPortfolioSettings
    {
        public ConservativePortfolioSettings()
        {
            InitialCashValue = Settings.Default.PortfolioValueInitial;
        }

        /// <summary>
        /// Die maximale Initiale Positionsgröße - 10% wenn noch kein Bestand in der Position, dann wird initial eine 10% Positoneröffnet - sprich nach der ersten Allokatoin sollten 10 stocks im Bestand sein
        /// </summary>
        public decimal MaximumInitialPositionSize => new decimal(0.1);

        /// <summary>
        /// Die maximale gesamte Positionsgröße - 33% - diese kann nach dem ersen aufstocken erreicht werden - 10% dann 20% dann 33%
        /// </summary>
        public decimal MaximumPositionSize => new decimal(0.33);

        /// <summary>
        /// Cash Puffer Größe 50 Bps
        /// </summary>
        public decimal CashPufferSize => new decimal(0.005);

        /// <summary>
        /// The Trading Interval of The Portfolio
        /// </summary>
        public TradingInterval Interval { get; set; } = TradingInterval.weekly;

        /// <summary>
        /// The default Trading Day in the Week
        /// </summary>
        public DayOfWeek TradingDay { get; set; } = DayOfWeek.Wednesday;

        /// <summary>
        /// der totale Investitionsgrad
        /// </summary>
        public decimal MaxTotaInvestmentLevel => (1 - (decimal)CashPufferSize);

        /// <summary>
        /// Der Initiale Portfolio Wert
        /// </summary>
        public decimal InitialCashValue { get; }

        /// <summary>
        /// Die Haltedauer bevor die Position abgeschichtet oder ausgetauscht werden darf
        /// </summary>
        public int MinimumHoldingPeriodeInDays { get; set; } = 14;

        /// <summary>
        /// ReplaceBufferPce +1 * Score dammit wird der aktuelle Score beim vergleichen mit einem anderen Kandidaten erhöht, 
        /// schichte nur in Positionen um die "deutlich vielversprechender" sind
        /// </summary>
        public decimal ReplaceBufferPct { get; set; } = new decimal(1);

        /// <summary>
        /// Dieser Puffer wird vom maximum der position Size abgezogen, ist der kandidat darunter wird er auf das maximum aufgestockt 
        /// sonst ist er bereits am maximum
        /// aktuell 5%
        /// </summary>
        public decimal MaximumPositionSizeBuffer { get; set; } = new decimal(0.05);

        public string LoggingPath { get; set; }
    }

    public class DefaultPortfolioSettings : IPortfolioSettings
    {
        public DefaultPortfolioSettings()
        {
            InitialCashValue = Settings.Default.PortfolioValueInitial;
        }

        /// <summary>
        /// Die maximale Initiale Positionsgröße - 10% wenn noch kein Bestand in der Position, dann wird initial eine 10% Positoneröffnet - sprich nach der ersten Allokatoin sollten 10 stocks im Bestand sein
        /// </summary>
        public decimal MaximumInitialPositionSize => new decimal(0.1);

        /// <summary>
        /// Die maximale gesamte Positionsgröße - 33% - diese kann nach dem ersen aufstocken erreicht werden - 10% dann 20% dann 33%
        /// </summary>
        public decimal MaximumPositionSize => new decimal(0.33);

        /// <summary>
        /// Cash Puffer Größe 50 Bps
        /// </summary>
        public decimal CashPufferSize => new decimal(0.005);

        /// <summary>
        /// The Trading Interval of The Portfolio
        /// </summary>
        public TradingInterval Interval { get; set; } = TradingInterval.weekly;

        /// <summary>
        /// The default Trading Day in the Week
        /// </summary>
        public DayOfWeek TradingDay { get; set; } = DayOfWeek.Wednesday;

        /// <summary>
        /// der totale Investitionsgrad
        /// </summary>
        public decimal MaxTotaInvestmentLevel => (1 - (decimal)CashPufferSize);

        /// <summary>
        /// Der Initiale Portfolio Wert
        /// </summary>
        public decimal InitialCashValue { get; }

        /// <summary>
        /// Die Haltedauer bevor die Position abgeschichtet oder ausgetauscht werden darf
        /// </summary>
        public int MinimumHoldingPeriodeInDays { get; set; } = 14;

        /// <summary>
        /// ReplaceBufferPce +1 * Score dammit wird der aktuelle Score beim vergleichen mit einem anderen Kandidaten erhöht, 
        /// schichte nur in Positionen um die "deutlich vielversprechender" sind
        /// </summary>
        public decimal ReplaceBufferPct { get; set; } = new decimal(0.5);

        /// <summary>
        /// Dieser Puffer wird vom maximum der position Size abgezogen, ist der kandidat darunter wird er auf das maximum aufgestockt 
        /// sonst ist er bereits am maximum
        /// aktuell 5%
        /// </summary>
        public decimal MaximumPositionSizeBuffer { get; set; } = new decimal(0.05);

        public string LoggingPath { get; set; }
    }
}
