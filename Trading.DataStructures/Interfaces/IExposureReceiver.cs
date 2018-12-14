using System;

namespace Trading.DataStructures.Interfaces
{
    public interface IExposureSettings
    {
        /// <summary>
        /// Das maximum der Aktienquote
        /// </summary>
        decimal MaximumAllocationToRisk { get; set; }

        /// <summary>
        /// Das minimum der Aktienquote (dient dafür, dass ich z.B: immer 25% Risko habe im Portfolio)
        /// </summary>
        decimal MinimumAllocationToRisk { get; set; }

        /// <summary>
        /// Der Pfad von dem aus der ExposureProvider die IndexDaten bekommt
        /// </summary>
        string IndicesDirectory { get; set; }

        /// <summary>
        /// der Handelstag, an dem das Portfolio immer neu allokiert wird
        /// </summary>
        DayOfWeek TradingDay { get; set; }
    }
}