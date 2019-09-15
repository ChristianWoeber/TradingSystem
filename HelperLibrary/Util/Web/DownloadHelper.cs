using HelperLibrary.Database.Enums;
using HelperLibrary.Interfaces;
using HelperLibrary.Yahoo;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace HelperLibrary.Util.Web
{
    public static class DownloadHelper
    {

        public static void Validate(IEnumerable<IDownloadModel> collection, ValidationType type)
        {

            throw new System.NotImplementedException("Achtung die Funktion wurde seitens google geändert, muss neu implemntiert werden");

            //await Task.Factory.StartNew(() =>
            // {
            //     var tickes = collection.Select(x => x.DbSecurity.Ticker).ToList();
            //     // build Requests
            //     var req = new YahooRequestBuilder(YahooRequestType.Single).
            //         Tickers(tickes).
            //         BuildUrls();

            //     foreach (var urlItem in req.Urls)
            //     {
            //         // parsing Requests
            //         var record = TextParser.GetSingleYahooLineHcMapping(DownloadData(urlItem.Url));
            //         var matchingSec = collection.FirstOrDefault(x => x.DbSecurity.Ticker == urlItem.Ticker);

            //         if (record != null)
            //         {
            //             if (matchingSec != null)
            //             {
            //                 if (record.Name.ContainsIc(matchingSec.DbSecurity.Name) || matchingSec.DbSecurity.Name.ContainsIc(record.Name) || record.Name.ContainsIc(matchingSec.DbSecurity.Description))
            //                 {
            //                     matchingSec.IsValid = true;
            //                     DataBaseQueryHelper.StoreOrUpdateValidationResult(matchingSec.DbSecurity.SecurityId, matchingSec.IsValid, type);
            //                 }
            //                 else
            //                 {
            //                     matchingSec.IsValid = false;
            //                     DataBaseQueryHelper.StoreOrUpdateValidationResult(matchingSec.DbSecurity.SecurityId, matchingSec.IsValid, type);
            //                 }
            //             }
            //         }
            //         else
            //         {
            //             matchingSec.IsValid = false;
            //             DataBaseQueryHelper.StoreOrUpdateValidationResult(matchingSec.DbSecurity.SecurityId, matchingSec.IsValid, type);
            //         }

            //     }
            // });
        }

        public static string DownloadData(string url)
        {

            //if no token found, refresh it
            if (string.IsNullOrEmpty(Token.Cookie) | string.IsNullOrEmpty(Token.Crumb))
            {
                if (!Token.Refresh("SPY"))
                    return DownloadData(url);
            }


            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add(HttpRequestHeader.Cookie, Token.Cookie);
                try
                {
                    var urlWithCrumb = url += Token.Crumb;
                    return wc.DownloadString(urlWithCrumb);
                }

                catch (WebException webEx)
                {
                    HttpWebResponse response = (HttpWebResponse)webEx.Response;

                    //Re-fecthing token
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Debug.Print(webEx.Message);
                        Token.Cookie = "";
                        Token.Crumb = "";
                        Debug.Print("Re-fetch");
                        return DownloadData(url);
                    }
                    else
                    {
                        throw;
                    }

                }
            }
        }
    }
}
