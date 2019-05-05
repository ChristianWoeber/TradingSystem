using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using HelperLibrary.Collections;
using HelperLibrary.Database.Models;
using HelperLibrary.Parsing;
using OfficeOpenXml;
using Trading.DataStructures.Interfaces;
using Trading.UI.Wpf.Models;

namespace Trading.UI.Wpf.Utils
{

    public static class BootStrapperFactory
    {
        public static IEnumerable<T> CreateCollectionFromFile<T>(string path)
        {
            return File.Exists(path)
                ? SimpleTextParser.GetListOfTypeFromFilePath<T>(path)
                : null;
        }



        private static readonly Dictionary<int, string> _idToNameCatalog = new Dictionary<int, string>();

        public static Dictionary<int, IPriceHistoryCollection> CreatePriceHistoryFromFile(string path, DateTime? start, DateTime? end = null)
        {
            if (start == null)
                start = new DateTime(2004, 01, 01);
            if (end == null)
                end = new DateTime(2018, 01, 01);

            var dic = new Dictionary<int, IPriceHistoryCollection>();

            if (!File.Exists(path))
                throw new ArgumentException(@"Am angegeben Pfad exisitert keine Datei !", path);

            using (var excel = new ExcelPackage(new FileStream(path, FileMode.Open)))
            {
                foreach (var sheet in excel.Workbook.Worksheets)
                {
                    var secName = sheet.Name;
                    var quotes = new List<ITradingRecord>();

                    for (int rowIdx = 0; rowIdx < sheet.Cells.Rows; rowIdx++)
                    {
                        //erste Spalte Datum 2te der preis
                        var dateVal = sheet.Cells[rowIdx + 1, 1].Value;
                        if (dateVal == null)
                            continue;

                        var date = Convert.ToDateTime(dateVal);
                        var price = Convert.ToDecimal(sheet.Cells[rowIdx + 1, 2].Value);

                        //alle früheren ignoriere ich
                        if (date <= start)
                            continue;
                        //alle späteren ebenfalls indem ich komplett aus der for breake
                        if (date > end)
                            break;

                        var quote = new TradingRecord
                        {
                            Asof = date,
                            Price = price,
                            AdjustedPrice = price,
                            SecurityId = sheet.Index,
                            Name = secName
                        };

                        quotes.Add(quote);
                    }
                    _idToNameCatalog.Add(sheet.Index, secName);
                    dic.Add(sheet.Index, PriceHistoryCollection.Create(quotes));
                }
            }
            return dic;
        }

        public static Dictionary<int, string> GetIdToNameDictionary()
        {
            return _idToNameCatalog;
        }

        public static Dictionary<int, IPriceHistoryCollection> CreatePriceHistoryFromSingleFiles(string path)
        {
            //der Return Value
            var dic = new Dictionary<int, IPriceHistoryCollection>();

            if (!Directory.Exists(path))
                throw new ArgumentException(@"Am angegeben Pfad exisitert keine Datei !", path);

            //Parallel For Each ist in diesem Fall um den Faktor der kerne schneller
            Parallel.ForEach(Directory.GetFiles(path, "*.csv"), file =>
            {
                if (string.IsNullOrWhiteSpace(file))
                    return;

                var fileName = Path.GetFileNameWithoutExtension(file);
                //Id und Name parsen aus dem File
                //var split = Path.GetFileNameWithoutExtension(file).Split('_');

                //der Idx des letzten underscores
                var idx = Path.GetFileNameWithoutExtension(file).LastIndexOf('_');

                //Wenn keine Id gefunden wurde die Security ignorieren
                if (idx == -1)
                    return;

                var name = fileName.Substring(0, idx).Trim('_');
                var id = Convert.ToInt32(fileName.Substring(idx, fileName.Length - idx).Trim('_'));
                //die TradingRecords auslesen
                var data = SimpleTextParser.GetListOfTypeFromFilePath<TradingRecord>(file);
                //settings erstellen
                var settings = new PriceHistorySettings { Name = name };
                //im dictionary merken
                dic.Add(id, PriceHistoryCollection.Create(data, settings));
            });

            return dic;
        }

    }
}

