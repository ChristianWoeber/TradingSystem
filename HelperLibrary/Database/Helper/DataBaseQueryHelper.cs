using System;
using System.Collections.Generic;
using System.Linq;
using HelperLibrary.Database.Models;
using MySql.Data.MySqlClient;
using HelperLibrary.Database.Enums;
using HelperLibrary.Extensions;
using System.Windows.Forms;
using HelperLibrary.Database.Interfaces;
using Trading.DataStructures.Interfaces;

namespace HelperLibrary.Database
{
    public class DataBaseQueryHelper
    {
        public static Dictionary<int, Security> GetAllSecurities()
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                return SQLCmd.Select("trading", "securities").
                    Fields("SECURITY_ID, NAME, TICKER, DESCRIPTION, ACTIVE, SECURITY_TYPE, CURRENCY, ISIN, SECTOR,INDEX_MEMBER_OF,COUNTRY").
                    QueryObjects<Security>().
                    ToDictionary(x => x.SecurityId);
            }
        }

        private static HashSet<string> _dbKeys;
        private static Dictionary<DateTime, EzbFxRecord> _fxDbKeys;

        public static List<Validation> GetValidations(int securityId)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                return SQLCmd.Select("trading", "validations").
                    Fields("ID_,SECURITY_ID,VALIDATION_TYPE,LAST_VALIDATION,IS_VALID").Equal("SECURITY_ID", securityId).
                    QueryObjects<Validation>().ToList();
            }
        }

        public static void InsertOrUpdateFxRecords(List<EzbFxRecord> data)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                if (_fxDbKeys == null)
                    LoadFxKeys();

                foreach (var fxRec in data)
                {
                    if (_fxDbKeys == null || !_fxDbKeys.ContainsKey(fxRec.AsOf))
                    {
                        SQLCmd.Insert("trading", "fx_data").Values("ASOF", fxRec.AsOf,
                            "USD", fxRec.Eur_Usd,
                            "GBP", fxRec.Eur_Gbp,
                            "JPY", fxRec.Eur_Jpy,
                            "CHF", fxRec.Eur_Chf,
                            "AUD", fxRec.Eur_Aud);
                    }
                    else
                    {
                        SQLCmd.Update("trading", "fx_data").
                            Values(
                            "USD", fxRec.Eur_Usd,
                            "GBP", fxRec.Eur_Gbp,
                            "JPY", fxRec.Eur_Jpy,
                            "CHF", fxRec.Eur_Chf,
                            "AUD", fxRec.Eur_Aud).Equal("ASOF", fxRec.AsOf);
                    }

                    SQLCmd.Execute();
                }
            }
        }

        public static Dictionary<int, string> GetIndicesCatalog()
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
                return SQLCmd.Select("TRADING", "SECURITIES").Fields("SECURITY_ID", "NAME").Equal("SECURITY_TYPE", "Index").QueryDictionary<int, string>();
        }

        public static void DeleteYahooRecord(ITradingRecord selectedRecord)
        {
            try
            {
                using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
                {
                    var cmd = SQLCmd.Delete("TRADING", "yahoo_data").Equal("SECURITY_ID", selectedRecord.SecurityId, "ASOF", selectedRecord.Asof);
                    SQLCmd.Execute();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static void LoadFxKeys()
        {
            _fxDbKeys = new Dictionary<DateTime, EzbFxRecord>();
            var data = SQLCmd.Select("trading", "fx_data").Fields("ASOF,USD,JPY,GBP,CHF,AUD").QueryObjects<EzbFxRecord>();
            foreach (var item in data)
            {
                if (!_fxDbKeys.ContainsKey(item.AsOf))
                    _fxDbKeys.Add(item.AsOf, item);
            }

        }

        public static void InsertOrUpdateSecurities(IDictionary<string, Security> securities)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                var dicSecs = SQLCmd.Select("trading", "securities").
                    Fields("SECURITY_ID, NAME, TICKER, DESCRIPTION, ACTIVE, SECURITY_TYPE, CURRENCY, ISIN, SECTOR,INDEX_MEMBER_OF").
                    IsNotNull("ISIN").
                    QueryObjects<Security>().
                    ToDictionary(x => x.ISIN);

                foreach (var sec in securities.Values)
                {
                    if (dicSecs.ContainsKey(sec.ISIN))
                    {
                        SQLCmd.Update("trading", "securities").Values(
                            "NAME", sec.Name,
                            "TICKER", sec.Ticker,
                            "DESCRIPTION", sec.Description,
                            "ACTIVE", sec.Active,
                            "SECURITY_TYPE", sec.SecurityType,
                            "CURRENCY", sec.Ccy,
                            "ISIN", sec.ISIN,
                            "SECTOR", sec.Sector,
                            "INDEX_MEMBER_OF", sec.IndexMemberOf,
                            "COUNTRY", sec.Country).Equal("ISIN", sec.ISIN);
                        SQLCmd.Execute();
                    }
                    else
                    {
                        SQLCmd.Insert("trading", "securities").Values(
                            "NAME", sec.Name,
                            "TICKER", sec.Ticker,
                            "DESCRIPTION", sec.Description,
                            "ACTIVE", sec.Active,
                            "SECURITY_TYPE", sec.SecurityType,
                            "CURRENCY", sec.Ccy,
                            "ISIN", sec.ISIN,
                            "SECTOR", sec.Sector,
                            "INDEX_MEMBER_OF", sec.IndexMemberOf,
                            "COUNTRY", sec.Country);
                        SQLCmd.Execute();
                    }
                }
            }
        }

        public static void UpdateOrStoreTransaction(Transaction transaction, bool testMode = false)
        {
            if (testMode)
            {
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static IEnumerable<ITransaction> GetCurrentPortfolio()
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                return SQLCmd.Call().Procedure("getCurrentPortfolio").QueryObjects<Transaction>();
            }
        }

        public static void DeleteDoubleEntriesFromValidationTable()
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                var items = SQLCmd.Select("trading", "validations").Fields("ID_", "SECURITY_ID", "VALIDATION_TYPE", "LAST_VALIDATION", "IS_VALID").QueryObjects<Validation>();
                var dic = items.ToDictionaryList(x => x.SecurityId);

                foreach (var values in dic.Values.Where(x => x.Count > 1))
                {
                    var ordered = values.OrderByDescending(x => x.LastValidationDateTime).ToList();

                    for (int i = 1; i < ordered.Count; i++)
                    {
                        DeleteFromValidationTable(ordered[i]);
                        SQLCmd.Execute();
                    }
                }
            }
        }

        public static Dictionary<int, List<YahooDataRecord>> SQLGetAllFirstAndLastItems(IEnumerable<object> secIds)
        {
            var dic = new Dictionary<int, List<YahooDataRecord>>();
            foreach (var id in secIds)
            {
                try
                {

                    var lastandFirst = SQLCmd.Call().Procedure("getSinglePriceHistoryFirstAndLastItem", id).QueryObjects<YahooDataRecord>();
                    if (!dic.ContainsKey((int)id))
                        dic.Add((int)id, new List<YahooDataRecord>());

                    dic[(int)id].AddRange(lastandFirst);

                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message);
                }
            }
            return dic;


        }

        public static IEnumerable<ITradingRecord> GetSinglePriceHistory(int secid, DateTime? start = null)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                if (start != null)
                {
                    var cmdDate = SQLCmd.Call().Procedure("getSinglePriceHistoryWithDateTime", secid, start);

                    foreach (var item in cmdDate.QueryObjects<YahooDataRecord>())
                        yield return item;

                }
                else
                {
                    var cmd = SQLCmd.Call().Procedure("getSinglePriceHistory", secid);
                    foreach (var item in cmd.QueryObjects<YahooDataRecord>())
                        yield return item;
                }

            }
        }

        private static void DeleteFromValidationTable(Validation validation)
        {
            SQLCmd.Delete("trading", "validations").Equal("ID_", validation.Id);
        }

        public static void StoreOrUpdateValidationResult(int securityId, bool validationResult, ValidationType type)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                var keys = SQLCmd.Select("trading", "validations").Fields("SECURITY_ID,VALIDATION_TYPE").QueryKeySet(typeof(int), typeof(int));

                if (keys != null && keys.Contains($"{securityId}_{(int)type}"))
                {
                    SQLCmd.Update("trading", "validations").Values(
                          "SECURITY_ID", securityId,
                          "VALIDATION_TYPE", (int)type,
                          "IS_VALID", validationResult == true ? 1 : 0,
                          "LAST_VALIDATION", DateTime.Now).
                          Equal("SECURITY_ID", securityId, "VALIDATION_TYPE", (int)type);
                }
                else
                {
                    SQLCmd.Insert("trading", "validations").Values(
                          "SECURITY_ID", securityId,
                          "VALIDATION_TYPE", (int)type,
                          "IS_VALID", validationResult == true ? 1 : 0,
                          "LAST_VALIDATION", DateTime.Now);

                }
                SQLCmd.Execute();
            }
        }

        public static void SaveSecurity(Security sec)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                SQLCmd.Update("trading", "securities").Values(
                            "NAME", sec.Name,
                            "TICKER", sec.Ticker,
                            "DESCRIPTION", sec.Description,
                            "ACTIVE", sec.Active,
                            "SECURITY_TYPE", sec.SecurityType,
                            "CURRENCY", sec.Ccy,
                            "ISIN", sec.ISIN,
                            "SECTOR", sec.Sector,
                            "INDEX_MEMBER_OF", sec.IndexMemberOf,
                            "COUNTRY", sec.Country).
                            Equal("SECURITY_ID", sec.SecurityId);
                SQLCmd.Execute();
            }
        }

        public static Dictionary<int, string> GetValidationTypeTable()
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                return SQLCmd.Select("Trading", "validation_types").Fields("ID_", "TYPE").QueryDictionary<int, string>();
            }
        }

        public static Dictionary<int, List<Validation>> GetValidationTable()
        {
            var dic = new Dictionary<int, List<Validation>>();

            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                foreach (var item in SQLCmd.Select("Trading", "validations").Fields("ID_", "SECURITY_ID", "VALIDATION_TYPE", "LAST_VALIDATION", "IS_VALID").QueryObjects<Validation>())
                {
                    if (!dic.ContainsKey(item.SecurityId))
                        dic.Add(item.SecurityId, new List<Validation>());

                    dic[item.SecurityId].Add(item);
                }
            }
            return dic;
        }

        public static Dictionary<int, List<YahooDataRecord>> GetDataTable()
        {
            var dic = new Dictionary<int, List<YahooDataRecord>>();
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                var cmd = SQLCmd.Select("Trading", "yahoo_data").Fields("ASOF", "SECURITY_ID", "CLOSE_PRICE", "ADJUSTED_CLOSE_PRICE");

                foreach (var item in cmd.QueryObjects<YahooDataRecord>())
                {
                    if (!dic.ContainsKey(item.SecurityId))
                        dic.Add(item.SecurityId, new List<YahooDataRecord>());

                    dic[item.SecurityId].Add(item);
                }
            }
            return dic;
        }

        public static int SelectIndexId(string keyword, string title)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                var dbInt = SQLCmd.Select("Trading", "Securities").Fields("SECURITY_ID").Equal("NAME", keyword).QuerySingle<int>();
                return dbInt == 0 ? SQLCmd.Select("Trading", "Securities").Fields("SECURITY_ID").Equal("NAME", title).QuerySingle<int>() : dbInt;
            }
        }

        public static List<string> GetTickers(bool onlyValidTickers = false)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                if (onlyValidTickers)
                {
                    var secIds = SQLCmd.Select("Trading", "validations").Fields("SECURITY_ID").Equal("VALIDATION_TYPE", 1, "IS_VALID", 1).QueryList<object>();
                    return SQLCmd.Select("Trading", "Securities").Fields("TICKER").InList("SECURITY_ID", secIds).QueryList<string>();

                }
                return SQLCmd.Select("Trading", "Securities").Fields("TICKER").QueryList<string>();

            }
        }

        public static int GetSecIdFromTicker(string ticker)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                return SQLCmd.Select("Trading", "Securities").Fields("SECURITY_ID").Equal("TICKER", ticker).QuerySingle<int>();
            }
        }
        // inserts the records into db or updates them
        public static void InsertOrUpdateSingleSecurityDataHistory(List<YahooDataRecord> records, IProgress<double> progress, int secId)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                // Lazy Loading dbKeys for mapping //
                if (_dbKeys == null)
                    _dbKeys = SQLCmd.Select("Trading", "yahoo_data").Fields("ASOF", "SECURITY_ID").QueryKeySet();

                var i = 0;

                foreach (var rec in records)
                {
                    if (rec == null)
                        continue;

                    var inputKey = $"{rec.Asof.Date}_{secId}";


                    if (_dbKeys.Contains(inputKey))
                    {
                        SQLCmd.Update("Trading", "yahoo_data").
                            Values("CLOSE_PRICE", rec.Price,
                                   "ADJUSTED_CLOSE_PRICE", rec.AdjustedPrice).
                            Equal("ASOF", rec.Asof,
                                  "SECURITY_ID", secId);
                    }
                    else
                    {
                        SQLCmd.Insert("Trading", "yahoo_data").
                               Values(
                               "ASOF", rec.Asof,
                               "CLOSE_PRICE", rec.Price,
                               "ADJUSTED_CLOSE_PRICE", rec.AdjustedPrice,
                               "SECURITY_ID", secId);
                    }


                    SQLCmd.Execute();

                    i++;
                    progress?.Report((double)i / records.Count);

                }
            }
        }



        // inserts the records into db or updates them
        public static void InsertOrUpdateMultipleSecurityDataPrices(List<YahooDataRecord> records, IProgress<double> progress)
        {
            using (SQLCmd.Connection = DataBaseFactory.Create(new MySqlConnection()))
            {
                // Lazy Loading dbKeys for mapping //
                if (_dbKeys == null)
                    _dbKeys = SQLCmd.Select("Trading", "yahoo_data").Fields("ASOF", "SECURITY_ID").QueryKeySet();

                var i = 0;

                foreach (var rec in records)
                {
                    if (rec == null)
                        continue;

                    var inputKey = $"{rec.Asof.Date}_{rec.SecurityId}";


                    if (_dbKeys.Contains(inputKey))
                    {
                        SQLCmd.Update("Trading", "yahoo_data").
                            Values("CLOSE_PRICE", rec.Price,
                                   "ADJUSTED_CLOSE_PRICE", rec.AdjustedPrice).
                            Equal("ASOF", rec.Asof,
                                  "SECURITY_ID", rec.SecurityId);
                    }
                    else
                    {
                        SQLCmd.Insert("Trading", "yahoo_data").
                               Values(
                               "ASOF", rec.Asof,
                               "CLOSE_PRICE", rec.Price,
                               "ADJUSTED_CLOSE_PRICE", rec.AdjustedPrice,
                               "SECURITY_ID", rec.SecurityId);
                    }


                    SQLCmd.Execute();

                    i++;
                    progress?.Report((double)i / records.Count);

                }
            }
        }

    }
}