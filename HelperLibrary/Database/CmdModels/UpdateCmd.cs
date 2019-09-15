using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database.CmdModels
{
    public class UpdateCmd : ISqlCmdText
    {
        public string CreateCmd(string db, string table)
        {
            return $"UPDATE {db}.{table} set ";
        }
    }
}
