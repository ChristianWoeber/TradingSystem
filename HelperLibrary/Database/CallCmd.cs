using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database
{
    internal class CallCmd : ISqlCmdText
    {
        public string CreateCmd(string db, string table)
        {
            return $"Call ";
        }
    }
}