using System.Collections.Generic;
using Trading.DataStructures.Interfaces;

namespace Trading.Core.Exposure
{
    /// <summary>
    /// Interface das den Blueprint für den DataProvider für den Exposure Watcher beeinhält
    /// kann etwerder von der File struktur oder aus der DB gequeriet werden
    /// </summary>
    public interface IExposureDataProvider
    {
        /// <summary>
        /// Methode um die Record bereitzustellen
        /// </summary>
        /// <returns></returns>
        IEnumerable<ITradingRecord> GetExposureRecords();
    }
}