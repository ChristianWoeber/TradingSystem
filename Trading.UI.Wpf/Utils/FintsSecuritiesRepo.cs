using System;
using System.IO;
using System.Linq;
using Arts.Financial;
using Trading.Core.Models;
using Trading.Parsing;

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

        public static Lazy<FINTS<double>> MsciWorldEur { get; } = new Lazy<FINTS<double>>(LoadMsciWorldEurHistory);

        private static FINTS<double> LoadMsciWorldEurHistory()
        {
            var path = Path.Combine(Globals.IndicesBasePath, "MSCIWorldEur.csv");
            return FINTS.Create(SimpleTextParser.GetListOfTypeFromFilePath<TradingRecord>(path)
                .Select(t => new Quote<double>(new SDate(t.Asof), (double)t.AdjustedPrice)));
        }

        public static Lazy<FINTS<double>> Dax { get; } = new Lazy<FINTS<double>>(LoadDaxHistory);

        private static FINTS<double> LoadDaxHistory()
        {
            var path = Path.Combine(Globals.IndicesBasePath, "Dax.csv");
            return FINTS.Create(SimpleTextParser.GetListOfTypeFromFilePath<TradingRecord>(path)
                .Select(t => new Quote<double>(new SDate(t.Asof), (double)t.AdjustedPrice)));
        }
    }
}
