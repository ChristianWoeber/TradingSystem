using System;
using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database
{
    internal class SelectCmd : ISqlCmdText
    {
        public string CreateCmd(string db, string table)
        {
            return $"SELECT @ from {db}.{table}";
        }
    }
}