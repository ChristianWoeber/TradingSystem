using System.ComponentModel;

namespace Trading.DataStructures.Enums
{
    public enum IndexType
    {
        [Description("Dax")]
        Dax,
        [Description("EuroStoxx")]
        EuroStoxx50,
        [Description("MSCIWorldEUR")]
        MsciWorldEur,
        [Description("S&P")]
        SandP500
    }
}