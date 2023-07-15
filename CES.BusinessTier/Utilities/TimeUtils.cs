using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CES.BusinessTier.Utilities
{
    public static class TimeUtils
    {
        public static DateTime ConvertUnixTimeToDateTime(long utcExpireDate)
        {
            var dateTimeInterval = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeInterval.AddSeconds(utcExpireDate).ToUniversalTime();

            return dateTimeInterval;
        }
        public static string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        public static string GetHoursTime(DateTime value)
        {
            return value.ToString("H:mm");
        }

        public static DateTime GetCurrentSEATime()
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime localTime = DateTime.Now;
            DateTime utcTime = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, tz);
            return utcTime;
        }

        public static DateTime ConvertToSEATime(DateTime value)
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime convertedTime = TimeZoneInfo.ConvertTime(value, tz);
            return convertedTime;
        }
        
        public static DateTime GetCurrentDate()
        {
            return DateTime.UtcNow.AddHours(7);
        }
        
        public static DateTime GetStartOfDate(this DateTime value)
        {
            return new DateTime(value.Year, value.Month, value.Day, 0, 0, 0);
        }

        public static DateTime GetEndOfDate(this DateTime value)
        {
            return new DateTime(value.Year, value.Month, value.Day, 23, 59, 59);
        }
        
        public static (DateTime, DateTime) GetLastAndFirstDateInCurrentMonth()
        {
            var now = GetCurrentSEATime();
            var first = new DateTime(now.Year, now.Month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            return (first, last);
        }
        
        public static string ToCronExpression(DateTime dateTime)
        {
            // Extract the individual components from the DateTime object
            int minute = dateTime.Minute;
            int hour = dateTime.Hour;
            int dayOfMonth = dateTime.Day;
            int month = dateTime.Month;
            int dayOfWeek = (int)dateTime.DayOfWeek;

            // Adjust dayOfWeek to match the cron format (Sunday starts at 0)
            if (dayOfWeek == 0)
                dayOfWeek = 7;

            // Build the cron expression string
            string cronExpression = $"{minute} {hour} {dayOfMonth} {month} {dayOfWeek}";

            return cronExpression;
        }
        
        public static long GetTimeStamp(DateTime date)
        {
            return (long)(date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }

        public static long GetTimeStamp()
        {
            return GetTimeStamp(DateTime.Now);
        }
        
        public static DateTime ConvertDateTimeToVietNamTimeZone()
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime localTime = DateTime.Now;
            DateTime utcTime = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, tz);
            return utcTime;
        }
    }
}
