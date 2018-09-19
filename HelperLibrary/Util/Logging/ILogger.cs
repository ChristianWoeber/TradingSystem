namespace HelperLibrary.Util.Logging
{
    public interface ILogger
    {
        object lockObj { get; }
        void Info(string logMesage);
        void Error(string logMesage);
        void Debug(string logMesage);
    }
}
