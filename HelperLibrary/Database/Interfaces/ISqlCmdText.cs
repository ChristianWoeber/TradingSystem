namespace HelperLibrary.Database.Interfaces
{
    public interface ISqlCmdText
    {
        string CreateCmd(string db, string table);
    }
}