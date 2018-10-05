using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HelperLibrary.Collections;
using HelperLibrary.Database.Interfaces;
using HelperLibrary.Interfaces;
using HelperLibrary.Parsing;
using OfficeOpenXml;
using Trading.DataStructures.Interfaces;
using TradingSystemTests.Models;

namespace TradingSystemTests.Helper
{
    public static class TestHelper
    {
        public static IEnumerable<T> CreateTestCollection<T>(string filename)
        {
            var data = (string)Resource.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(filename) ?? throw new InvalidOperationException("Achtung kein File gefunden !! -- FileName:" + filename));

            if (data != null)
                return SimpleTextParser.GetListOfType<T>(data);

            if (File.Exists(filename))
                return SimpleTextParser.GetListOfTypeFromFilePath<T>(filename);


            throw new MissingMemberException("Die Datei konnte nicht gefunden werden " + filename);

        }

        public static IEnumerable<TestQuote> CreateTestCollection(string fileName, int securityId = 0)
        {
            var data = (string)Resource.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(fileName) ?? throw new InvalidOperationException("Achtung kein File gefunden !! -- FileName:" + fileName));

            if (data == null)
                throw new MissingMemberException("Die Datei konnte nicht gefunden werden " + fileName);

            var split = data.Split(Environment.NewLine.ToCharArray());

            foreach (var line in split)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var item = line.Split(';');
                var date = DateTime.Parse(item[0]);
                var price = Convert.ToDecimal(item[1]);

                yield return new TestQuote
                {
                    Asof = date,
                    Price = price,
                    AdjustedPrice = price,
                    SecurityId = securityId
                };
            }
        }

        public static Dictionary<int, IPriceHistoryCollection> CreateTestDictionary(string fileName, DateTime? start = null, DateTime? end = null)
        {
            var data = (byte[])Resource.ResourceManager.GetObject(Path.GetFileNameWithoutExtension(fileName) ?? throw new InvalidOperationException("Achtung kein File gefunden !! -- FileName:" + fileName));

            if (data == null)
                throw new MissingMemberException("Die Datei konnte nicht gefunden werden " + fileName);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Trace.TraceInformation("Start der StopUhr");

            if (start == null)
                start = new DateTime(2004, 01, 01);
            if (end == null)
                end = new DateTime(2010, 01, 01);

            var dic = new Dictionary<int, IPriceHistoryCollection>();

            using (var excel = new ExcelPackage(new MemoryStream(data)))
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

                        var quote = new TestQuote
                        {
                            Asof = date,
                            Price = price,
                            AdjustedPrice = price,
                            SecurityId = sheet.Index,
                            Name = secName
                        };

                        quotes.Add(quote);
                    }
                    dic.Add(sheet.Index, new PriceHistoryCollection(quotes));
                }
            }
            stopwatch.Stop();
            Trace.TraceInformation($"Dauer des Excel parsens in Sekunden: {stopwatch.Elapsed.TotalSeconds:N}");
            return dic;
        }

    }
}
