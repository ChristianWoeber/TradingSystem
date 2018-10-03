using System;
using HelperLibrary.Database.Interfaces;
using HelperLibrary.Trading;

namespace HelperLibrary.Interfaces
{
    public interface IScoringProvider : IPriceHistoryStorageProvider
    {

        /// <summary>
        /// The Method that Returns the Scoring Result
        /// </summary>
        /// <param name="secId">the id of the security</param>
        /// <param name="date">the start date for the Calculations</param>
        IScoringResult GetScore(int secId, DateTime date);



        /// <summary>
        /// Returns then ITradingRecord at the given Date
        /// </summary>
        /// <param name="securityId">the security id</param>
        /// <param name="asof">The Datetime of the record</param>
        /// <returns></returns>
        ITradingRecord GetTradingRecord(int securityId, DateTime asof);     
    }
}