using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Trading.Calculation.Collections;
using Trading.Core.Models;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;

namespace PriceHistoriesConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = "";
            if (args.Length >= 1)
            {
                path = args[0];
            }

            var dic = new Dictionary<int, IPriceHistoryCollection>();

            if (!Directory.Exists(path))
                throw new ArgumentException(@"Am angegeben Pfad exisitert keine Datei !", path);

            var sw = Stopwatch.StartNew();
            sw.Start();

            foreach (var file in Directory.GetFiles(path, "*.csv"))
            {
                if (string.IsNullOrWhiteSpace(file))
                    return;

                Console.WriteLine("aktuelles file: " + file);

                //der FileName
                var fileName = Path.GetFileNameWithoutExtension(file);

                //der Idx des letzten underscores
                var idx = Path.GetFileNameWithoutExtension(file).LastIndexOf('_');

                //Wenn keine Id gefunden wurde die Security ignorieren
                if (idx == -1)
                    continue;

                //der Name des Wertpapiers
                var name = fileName.Substring(0, idx).Trim('_');
                //die Id
                var id = Convert.ToInt32(fileName.Substring(idx, fileName.Length - idx).Trim('_'));
                //die TradingRecords auslesen
                var data = SimpleTextParser.GetItemsOfTypeFromFilePath<TradingRecord>(file);
                //settings erstellen
                var settings = new PriceHistorySettings { Name = name };
                //im dictionary merken

                dic.Add(id, PriceHistoryCollection.Create(data, settings));
                Console.WriteLine(name + " " + id + " wurde hinzugefügt");
                Console.WriteLine("aktueller Count: " + dic.Count);
            }

            sw.Stop();
            Trace.TraceInformation($"Dauer des Ladevorgangs in Sekunden: {sw.Elapsed.TotalSeconds:N}");

        }
    }
}
