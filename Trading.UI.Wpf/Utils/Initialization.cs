using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Lib.Util.CmdParser;
using HelperLibrary.Collections;
using HelperLibrary.Database.Interfaces;
using HelperLibrary.Database.Models;
using HelperLibrary.Interfaces;
using HelperLibrary.Parsing;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Trading.UI.Wpf.Models;

namespace Trading.UI.Wpf.Utils
{

    public static class Factory
    {
        public static IEnumerable<T> CreateCollectionFromFile<T>(string path)
        {
            return File.Exists(path)
                ? SimpleTextParser.GetListOfTypeFromFilePath<T>(path)
                : null;
        }

      

        private static readonly Dictionary<int, string> _idToNameCatalog = new Dictionary<int, string>();

        public static Dictionary<int, IPriceHistoryCollection> CreatePriceHistoryFromFile(string path, DateTime? start, DateTime? end)
        {
            if (start == null)
                start = new DateTime(2004, 01, 01);
            if (end == null)
                end = new DateTime(2010, 01, 01);

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

                        var quote = new TradingRecord()
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
                    dic.Add(sheet.Index, new PriceHistoryCollection(quotes));
                }
            }
            return dic;
        }

        public static Dictionary<int, string> GetIdToNameDictionary()
        {
            return _idToNameCatalog;
        }
    }
}

