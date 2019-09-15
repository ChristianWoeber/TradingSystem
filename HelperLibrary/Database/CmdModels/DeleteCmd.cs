using HelperLibrary.Database.Interfaces;

namespace HelperLibrary.Database.CmdModels
{
    public class DeleteCmd : ISqlCmdText
    {
        public string CreateCmd(string db, string table)
        {
            return $"Delete from {db}.{table}";
        }
    }
}
