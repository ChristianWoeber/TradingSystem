namespace HelperLibrary.Database.Interfaces
{
    public interface ISqlValueCmd
    {
        string CreateCmd(string field = null, params object[] values);
    }
}