﻿using System;
using Trading.DataStructures.Enums;

namespace HelperLibrary.Interfaces
{
    /// <summary>
    /// Interface Defining the Portfolio Settings
    /// </summary>
    public interface IPortfolioSettings
    {

        /// <summary>
        /// die initiale Positionsgröße einer Position, die größe mit der die initiale Position eröffnet wird
        /// </summary>
        decimal MaximumInitialPositionSize { get; }


        /// <summary>
        /// die maximale Postionsgröße pro Position
        /// </summary>
        decimal MaximumPositionSize { get; }


        /// <summary>
        /// die Puffergröße die verbleiben soll (nach maximalen investitionsgrad
        /// </summary>
        decimal CashPufferSize { get; }

        /// <summary>
        /// der Handelstag, an dem das Portfolio immer neu allokiert wird
        /// </summary>
        DayOfWeek TradingDay { get; set; }

        /// <summary>
        /// das Trading Interval wie of gehandelt wird
        /// </summary>
        TradingInterval Interval { get; set; }

        /// <summary>
        /// der maximale Investitonsgrad des Portfolios
        /// </summary>
        decimal MaxTotaInvestmentLevel { get; }

        /// <summary>
        /// der Initiale Portfolio Wert zum Starten
        /// </summary>
        decimal InitialCashValue { get; }

        /// <summary>
        /// die mindest Halte dauer einer Position
        /// </summary>
        int MinimumHoldingPeriodeInDays { get; set; }

        /// <summary>
        /// Dieser Puffer in % wird zum aktuellen Score des Kandiaten hinzugefügt, nur wenn der Besser ist wird umgeschichtet
        /// </summary>
        decimal ReplaceBufferPct { get; set; }

        /// <summary>
        /// Dieser Puffer wird vom maximum der position Size abgezogen, ist der kandidat darunter wird er auf das maximum aufgestockt 
        /// sonst ist er bereits am maximum
        /// </summary>
        decimal MaximumPositionSizeBuffer { get; set; }

        string LoggingPath { get; set; }
    }
}
