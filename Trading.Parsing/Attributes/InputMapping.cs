using System;

namespace Trading.Parsing.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InputMapping : Attribute
    {
        /// <summary>
        /// die Keywords nach den gesucht werden soll
        /// </summary>
        public string[] KeyWords { get; set; }

        /// <summary>
        /// der Index der Spalte beim Schreiben 
        /// </summary>
        public int SortIndex { get; set; }
    }
}
