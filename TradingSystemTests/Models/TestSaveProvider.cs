using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Trading.Core.Models;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;

namespace TradingSystemTests.Models
{
    /// <summary>
    /// Die Test Klasse, speichert die Transaktionen als Csv und zeigt das File an
    /// </summary>
    public class TestSaveProvider : ISaveProvider
    {
        private readonly string _filename;
        private readonly bool _showInFile;

        public TestSaveProvider(string filename, bool showInFile = false)
        {
            _filename = filename;
            _showInFile = showInFile;
        }

        public string TempPath { get; set; }

        public void Save(IEnumerable<ITransaction> items)
        {
            //der fileName
            TempPath = Path.Combine(Path.GetTempPath(), _filename);

            //ich schreibe ins File, allerdings nur die neuen items
            SimpleTextParser.AppendToFile(items.Cast<Transaction>().Where(x=>x.IsTemporary), TempPath);

            //anzeigen
            if (_showInFile)
                Process.Start(TempPath);
        }

        /// <summary>
        /// Methode um den Rebalance Score, swoie den Performance Score zu speichern und zu tracen
        /// </summary>
        /// <param name="portfolioManagerTemporaryCandidates"></param>
        /// <param name="portfolioManagerTemporaryPortfolio"></param>
        public void SaveScoring(Dictionary<int, ITradingCandidate> portfolioManagerTemporaryCandidates,
            ITemporaryPortfolio portfolioManagerTemporaryPortfolio)
        {
            throw new System.NotImplementedException();
        }
    }
}