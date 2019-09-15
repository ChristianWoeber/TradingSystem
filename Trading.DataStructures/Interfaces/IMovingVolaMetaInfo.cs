namespace Trading.DataStructures.Interfaces
{
    public interface IMovingVolaMetaInfo
    {
        /// <summary>
        /// die tägliche Volatilität
        /// </summary>
        decimal DailyVolatility { get; }

        /// <summary>
        /// das arithmetrische MIttel der daily Returns
        /// </summary>
        decimal AverageReturn { get; }

        /// <summary>
        /// die Varianz => Achtung ist schon durch N-1 bereiningt
        /// </summary>
        decimal Variance { get; }


    }
}