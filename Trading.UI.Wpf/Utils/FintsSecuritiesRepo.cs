﻿using System;
using System.IO;
using System.Linq;
using Arts.Financial;
using HelperLibrary.Database.Models;
using HelperLibrary.Parsing;

namespace Trading.UI.Wpf.Utils
{
    public static class FintsSecuritiesRepo
    {
        public static Lazy<FINTS<double>> Eurostoxx50 { get; } = new Lazy<FINTS<double>>(LoadEuroStoxx50History);

        private static FINTS<double> LoadEuroStoxx50History()
        {
            var path = Path.Combine(Globals.IndicesBasePath, "EuroStoxx50.csv");
            return FINTS.Create(SimpleTextParser.GetListOfTypeFromFilePath<TradingRecord>(path)
                .Select(t => new Quote<double>(new SDate(t.Asof), (double)t.AdjustedPrice)));
        }       
    }
}
