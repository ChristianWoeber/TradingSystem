using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trading.Core.Extensions;
using Trading.Core.Models;
using Trading.DataStructures.Enums;
using Trading.DataStructures.Interfaces;
using Trading.Parsing;

namespace Trading.Core.Exposure
{
    /// <summary>
    /// die Klasse bekommt die Records über die Filestruktur
    /// </summary>
    public class FileExposureDataProvider : IExposureDataProvider
    {
        private readonly Dictionary<IndexType, List<TradingRecord>> _records = new Dictionary<IndexType, List<TradingRecord>>();
        private readonly string _filePath;

        public FileExposureDataProvider(string filePath, IndexType type= IndexType.MsciWorldEur)
        {
            Type = type;
            _filePath = filePath;
            Initialize();
        }

        /// <summary>
        /// der konkrete Index der verwendet werden soll
        /// </summary>
        public IndexType Type { get; }


        private void Initialize()
        {
            if (string.IsNullOrWhiteSpace(_filePath))
                throw new ArgumentNullException(nameof(_filePath), "Achtung es wurde kein Pfad für die History der Indizes angegeben");

            var file = Directory.GetFiles(_filePath).FirstOrDefault(x => x.ContainsIc(Type.ToDescription()));
            if (string.IsNullOrWhiteSpace(file))
                throw new ArgumentNullException(nameof(file), $"Achtung es wurde kein File für den Index im angegebenen Pfad {_filePath} gefunden");

            var records = SimpleTextParser.GetListOfTypeFromFilePath<TradingRecord>(file);
            _records.Add(Type, records);
        }

        /// <summary>
        /// Methode um die Record bereitzustellen
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ITradingRecord> GetExposureRecords()
        {
            return _records.TryGetValue(Type, out var records) ? records : null;
        }
    }
}