namespace Trading.DataStructures.Interfaces
{
    public interface ITradingRecord : IPriceRecord
    {
        int SecurityId { get; set; }

        string Name { get; set; }
    }
}
