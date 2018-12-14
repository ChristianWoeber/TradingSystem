using System;
using System.Collections.Generic;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Trading.PortfolioManager.Settings
{
    public class DefaultSaveProvider : ISaveProvider
    {
        public void Save(IEnumerable<ITransaction> items)
        {
            throw new NotImplementedException();
        }
    }


    public class ConservativePortfolioSettings : IPortfolioSettings
    {
        public ConservativePortfolioSettings()
        {
            InitialCashValue = HelperLibrary.Settings.Default.PortfolioValueInitial;
            MaximumAllocationToRisk = 1;
            MinimumAllocationToRisk = new decimal(0.2);
            //2% Toleranz
            AllocationToRiskBuffer = new decimal(0.02);
            MinimumPositionSizePercent = new decimal(0.02);
        }

        /// <summary>
        /// Die maximale Initiale Positionsgröße - 10% wenn noch kein Bestand in der Position, dann wird initial eine 10% Positoneröffnet - sprich nach der ersten Allokatoin sollten 10 stocks im Bestand sein
        /// </summary>
        public decimal MaximumInitialPositionSize { get; set; } = new decimal(0.1);

        /// <summary>
        /// Die maximale gesamte Positionsgröße - 33% - diese kann nach dem ersen aufstocken erreicht werden - 10% dann 20% dann 33%
        /// </summary>
        public decimal MaximumPositionSize { get; set; } = new decimal(0.33);

        /// <summary>
        /// Cash Puffer Größe 50 Bps
        /// </summary>
        public decimal CashPufferSizePercent { get; set; } = new decimal(0.005);

        /// <summary>
        /// The Trading Interval of The Portfolio
        /// </summary>
        public TradingInterval Interval { get; set; } = TradingInterval.Weekly;

        /// <summary>
        /// The default Trading Day in the Week
        /// </summary>
        public DayOfWeek TradingDay { get; set; } = DayOfWeek.Wednesday;

        /// <summary>
        /// der totale Investitionsgrad
        /// </summary>
        public decimal MaxTotaInvestmentLevel => (1 - (decimal)CashPufferSizePercent);

        /// <summary>
        /// Der Initiale Portfolio Wert
        /// </summary>
        public decimal InitialCashValue { get; set; }

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
        public decimal AllocationToRiskBuffer { get; set; }
        public decimal MinimumPositionSizePercent { get; set; }
        public string IndicesDirectory { get; set; }

        /// <summary>
        /// das Maximum der aktienquote
        /// </summary>
        public decimal MaximumAllocationToRisk { get; set; }

        /// <summary>
        /// das Minimum der aktienquote
        /// </summary>
        public decimal MinimumAllocationToRisk { get; set; }
    }

    public class DefaultPortfolioSettings : IPortfolioSettings
    {
        public DefaultPortfolioSettings()
        {
            InitialCashValue = HelperLibrary.Settings.Default.PortfolioValueInitial;
            MaximumAllocationToRisk = 1;
            AllocationToRiskBuffer = new decimal(0.02);
            MinimumPositionSizePercent = (decimal)2 / 100;

        }

        /// <summary>
        /// Die maximale Initiale Positionsgröße - 10% wenn noch kein Bestand in der Position, dann wird initial eine 10% Positoneröffnet - sprich nach der ersten Allokatoin sollten 10 stocks im Bestand sein
        /// </summary>
        public decimal MaximumInitialPositionSize { get; set; } =
        new decimal(0.1);

        /// <summary>
        /// Die maximale gesamte Positionsgröße - 33% - diese kann nach dem ersen aufstocken erreicht werden - 10% dann 20% dann 33%
        /// </summary>
        public decimal MaximumPositionSize { get; set; } =
        new decimal(0.33);

        /// <summary>
        /// Cash Puffer Größe 50 Bps
        /// </summary>
        public decimal CashPufferSizePercent { get; set; } = new decimal(0.005);

        /// <summary>
        /// The Trading Interval of The Portfolio
        /// </summary>
        public TradingInterval Interval { get; set; } = TradingInterval.Weekly;

        /// <summary>
        /// The default Trading Day in the Week
        /// </summary>
        public DayOfWeek TradingDay { get; set; } = DayOfWeek.Wednesday;

        /// <summary>
        /// der totale Investitionsgrad
        /// </summary>
        public decimal MaxTotaInvestmentLevel => (1 - (decimal)CashPufferSizePercent);

        /// <summary>
        /// Der Initiale Portfolio Wert
        /// </summary>
        public decimal InitialCashValue { get; set; }

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

        /// <summary>
        /// Der Pfad in den gelogget werden soll
        /// </summary>
        public string LoggingPath { get; set; }

        /// <summary>
        /// die Puffergröße für das hin-Allokieren zur maximalen bzw. minimalen Aktienquote
        /// </summary>
        public decimal AllocationToRiskBuffer { get; set; }

        /// <summary>
        /// die Minimale Positionsgröße in % 
        /// </summary>
        public decimal MinimumPositionSizePercent { get; set; }

        /// <summary>
        /// der Pfad in dem die Daten zu den Indices liegen
        /// </summary>
        public string IndicesDirectory { get; set; }

        /// <summary>
        /// das Maximum der Aktienquote
        /// </summary>
        public decimal MaximumAllocationToRisk { get; set; }

        /// <summary>
        /// das Minimum der Aktienquote
        /// </summary>
        public decimal MinimumAllocationToRisk { get; set; }
    }
}
