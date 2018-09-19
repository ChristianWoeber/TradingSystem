

using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database
{
    public class InsertCmd : ISqlCmdText
    {
        public string CreateCmd(string db, string table)
        {
            return $"INSERT INTO {db}.{table}";
        }
    }
}