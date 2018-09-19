using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HelperLibrary.Database.Models;
using HelperLibrary.Interfaces;
using HelperLibrary.Parsing;

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

        public void Save(IEnumerable<TransactionItem> items)
        {
            //der fileName
            TempPath = Path.Combine(Path.GetTempPath(), _filename);

            //ich schreibe ins File, allerdings nur die neuen items
            SimpleTextParser.AppendToFile(items.Where(x=>x.IsTemporary), TempPath);

            //anzeigen
            if (_showInFile)
                Process.Start(TempPath);
        }
    }
}