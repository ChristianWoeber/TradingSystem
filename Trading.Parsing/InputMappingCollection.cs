using System;
using System.Collections.Generic;
using System.Linq;

namespace Trading.Parsing
{
    public class InputMappingCollection : Dictionary<string, InputMapper>
    {
        private string _rowHeader;
        private List<InputMapper> _matchedFields;
        private string _rowHeaderFromIndex;
        private static volatile object _lockObj = new object();

        public InputMappingCollection(string delimiter = ";") : base(StringComparer.OrdinalIgnoreCase)
        {
            Delimiter = delimiter;
        }

        /// <summary>
        /// der Seperator des Csv
        /// </summary>
        public string Delimiter { get; }

        /// <summary>
        /// der Spalten Header, wir nur einmal erstellt
        /// </summary>
        public string RowHeader => _rowHeader ?? CreateRowHeader();

        /// <summary>
        /// Gibt an ob bereits Mappings bestehen
        /// </summary>
        public bool HasMappings => this.Any(x => x.Value.HasMapping);

        /// <summary>
        /// Gitb den RowHeader au Basis des MatchingIdex zurück
        /// </summary>
        public string RowHeaderFromMatchingIndex
        {
            get
            {
                if (_rowHeaderFromIndex == null)
                {
                    lock (_lockObj)
                    {
                        //return _rowHeaderFromIndex ?? (_rowHeaderFromIndex = _matchedFields.OrderBy(x => x.MatchingIndex)
                        //           .Select(x => x.KeyWord)
                        //           .Aggregate((a, b) => a + Delimiter + b));

                        _rowHeaderFromIndex = _matchedFields.OrderBy(x => x.MatchingIndex)
                                   .Select(x => x.KeyWord)
                                 .Aggregate((a, b) => a + Delimiter + b);
                        return _rowHeaderFromIndex;
                    }

                }
                return _rowHeaderFromIndex;
            }
        }

        /// <summary>
        /// Enumeriert alle bereits sortierten und gemappten Felder 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<InputMapper> EnumMatchedFields()
        {
            EnusureMatchedFieldsAreLoaded();
            foreach (var mapper in _matchedFields)
                yield return mapper;

        }

        /// <summary>
        /// Stellt sicher dass die mated Fields sortiert sind
        /// </summary>
        private void EnusureMatchedFieldsAreLoaded()
        {
            if (_matchedFields == null || _matchedFields.Count == 0)
                _matchedFields = Values.Where(x => x.HasMapping).OrderBy(x => x.SortIndex).ToList();
        }

        /// <summary>
        /// erstellt den Header
        /// </summary>
        /// <returns></returns>
        private string CreateRowHeader()
        {
            EnusureMatchedFieldsAreLoaded();
            //Wenn ich kein
            if (_matchedFields.Count == 0)
                return null;

            return _rowHeader = _matchedFields.Select(x => x.KeyWord)
                .Aggregate((a, b) => a + Delimiter + b);
        }

        /// <summary>
        /// erstellt den RowValue auf basss des objekts
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="convertFunc">methode zum konvertieren</param>
        /// <returns></returns>
        public string CreateRowFromValue(object obj, Func<object, string> convertFunc)
        {
            EnusureMatchedFieldsAreLoaded();
            return _matchedFields.Select(x => convertFunc(x.GetterFunc(obj)))
                .Aggregate((a, b) => a + Delimiter + b);
        }
    }
}