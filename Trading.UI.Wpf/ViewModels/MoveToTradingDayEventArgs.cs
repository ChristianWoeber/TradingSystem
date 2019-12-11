using System;

namespace Trading.UI.Wpf.ViewModels
{
    /// <summary>
    /// Event args für das Navigieren mit dem Corsor im Main Chat Control
    /// </summary>
    public class MoveToTradingDayEventArgs
    {
        /// <summary>
        /// Der Tading Day
        /// </summary>
        public DayOfWeek TradingDay { get; }

        /// <summary>
        /// Flag das angibt, in welche Richtung navigiert werden soll
        /// </summary>
        public bool IsNext { get; }

        public MoveToTradingDayEventArgs(DayOfWeek tradingDay, bool isNext = true)
        {
            TradingDay = tradingDay;
            IsNext = isNext;
        }
    }
}