using System;

namespace Trading.Calculation.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsOlderThanMinutes(this DateTime dt, int minutes)
        {
            if (dt.AddMinutes(minutes) < DateTime.Now)
                return true;

            return false;
        }

        public static bool IsOlderThanDays(this DateTime dt, int days)
        {
            if (dt.AddDays(days) < DateTime.Now)
                return true;

            return false;
        }

        public static double ToUnixSeconds(this DateTime dt)
        {
            if (dt <= DateTime.MinValue)
                return -1;
            //the unix start was the 01.01.1970 00:00//
            var unixStartDateTime = new DateTime(1970, 1, 1, 0, 0, 0);
            //substart current date from start and return the seconds//
            return (dt - unixStartDateTime).TotalSeconds;
        }

        public static DateTime GetFirstDateOfYear(this DateTime date)
        {
            return new DateTime(date.Year, 1, 1);
        }

        /// <summary>
        ///     Gibt die Anzahl der tage des aktuellen Monats zurück
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetDaysOfCurrentMonth(this DateTime date)
        {
            switch (date.Month)
            {
                case 4:
                case 6:
                case 9:
                case 11:
                    return 30;
                case 2:
                    return DateTime.IsLeapYear(date.Year) ? 29 : 28;
                default:
                    return 31;
            }
        }

        /// <summary>
        ///     Gibt den ersten Tag des aktuellen Monats zurück
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetFirstDayOfCurrentMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        /// <summary>
        ///     Überprüft ob das aktuelle datum der letzte Tag des Monats ist
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool IsUltimo(this DateTime date)
        {
            return GetUltimo(date, false) == date.Date;
        }

        /// <summary>
        ///     Überprüft ob das aktuelle datum der letzte Wochentag des Monats ist
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool IsBusinessDayUltimo(this DateTime date)
        {
            return GetUltimo(date) == date.Date;
        }

        /// <summary>
        ///     Gibt das nächste Ultimo zurück
        /// </summary>
        /// <param name="date"></param>
        /// <param name="getBussinesDay">
        ///     True wenn der letzte Wochentag des Monats zurück gegeben werden soll. False wenn auch Sa
        ///     und So erlaubt sind
        /// </param>
        /// <returns></returns>
        public static DateTime GetNextUltimo(this DateTime date, bool getBussinesDay = true)
        {
            return GetUltimo(date, getBussinesDay, 1);
        }

        /// <summary>
        ///     Gibt das letzte Ultimo zurück
        /// </summary>
        /// <param name="date"></param>
        /// <param name="getBussinesDay">
        ///     True wenn der letzte Wochentag des Monats zurück gegeben werden soll. False wenn auch Sa
        ///     und So erlaubt sind
        /// </param>
        /// <returns></returns>
        public static DateTime GetLastUltimo(this DateTime date, bool getBussinesDay = true)
        {
            var dt = new DateTime(date.Year, date.Month, 1).AddDays(-1);

            if (!getBussinesDay)
                return dt;


            if (dt.DayOfWeek == DayOfWeek.Saturday)
                return dt.AddDays(-1);
            return dt.DayOfWeek == DayOfWeek.Sunday ? dt.AddDays(-2) : dt;
        }

        /// <summary>
        ///     Überspringt die Anzahl der Monate in skipMonth und gibt dann das Ultimo zurück
        /// </summary>
        /// <param name="date"></param>
        /// <param name="skipMonth">Anzahl der monate die überspungen werden sollen (-1 gibt z.B: das vorletzte Ultimo zurück)</param>
        /// <param name="getBussinesDay">
        ///     True wenn der letzte Wochentag des Monats zurück gegeben werden soll. False wenn auch Sa
        ///     und So erlaubt sind
        /// </param>
        /// <returns></returns>
        public static DateTime GetUltimo(this DateTime date, bool getBussinesDay = true, int skipMonth = 0)
        {
            var dt = new DateTime(date.Year, date.Month, 1).AddMonths(skipMonth + 1).AddDays(-1);

            if (!getBussinesDay)
                return dt;

            if (dt.DayOfWeek == DayOfWeek.Saturday)
                return dt.AddDays(-1);
            return dt.DayOfWeek == DayOfWeek.Sunday ? dt.AddDays(-2) : dt;
        }

        /// <summary>
        /// Gibt den nächsten oder den vorherigen BusinessDay zurück
        /// </summary>
        /// <param name="date">das source Datum</param>
        /// <param name="next">bie True => der nächste, sonst der voherige Werktag</param>
        /// <returns></returns>
        public static DateTime GetBusinessDay(this DateTime date, bool next = true)
        {
            var modifier = next ? 1 : -1;
            var tempDate = date.AddDays(next ? 1 * modifier : 0);

            switch (tempDate.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    if (!next)
                        return tempDate.AddDays(3 * modifier);
                    else
                        break;
                case DayOfWeek.Friday:
                    if (next)
                        return tempDate.AddDays(3 * modifier);
                    else
                        break;
                case DayOfWeek.Sunday:
                    return !next
                        ? tempDate.AddDays(2 * modifier)
                        : tempDate.AddDays(1 * modifier);
                case DayOfWeek.Saturday:
                    if (!next)
                        break;
                    else
                        return tempDate.AddDays(1 * modifier);
            }
            return tempDate;
        }



        /// <summary>
        ///     Gibt den nächsten Montag zurück
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetNextMonday(this DateTime date)
        {
            return GetDateOfNextWeekday(date, DayOfWeek.Monday);
        }

        /// <summary>
        ///     Gibt den nächsten Freitag zurück
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime GetNextFriday(this DateTime date)
        {
            return GetDateOfNextWeekday(date, DayOfWeek.Friday);
        }

        /// <summary>
        ///     Gibt den darauf folgenden Wochentag des Datums zurück
        /// </summary>
        /// <param name="date"></param>
        /// <param name="weekday"></param>
        /// <returns></returns>
        public static DateTime GetDateOfNextWeekday(this DateTime date, DayOfWeek weekday)
        {
            return date.Date.AddDays(WeekdaysDiff(date.DayOfWeek, weekday));
        }

        /// <summary>
        ///     Gibt die Wochentage zurück zwischen den beiden Datum (zB. Montag und Freitag == 4)
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static int WeekdaysDiff(this DayOfWeek from, DayOfWeek to)
        {
            if (from == to)
                return 0;

            var dayFrom = (int)@from;
            if (dayFrom == 0) //Sonntag
                dayFrom = 7;

            var dayTo = (int)to;
            if (dayTo == 0) //Sonntag
                dayTo = 7;


            if (dayFrom > dayTo)
                dayTo += 7;

            return dayTo - dayFrom;
        }

        public static bool IsBetween(this DateTime source, DateTime? From, DateTime? To)
        {
            if (From == null || From.Value <= DateTime.MinValue)
                return false;

            return source > From && source < (To ?? DateTime.Today);
        }

    }
}
