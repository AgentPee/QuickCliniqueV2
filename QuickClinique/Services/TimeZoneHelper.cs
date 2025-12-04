using System;
using System.Globalization;

namespace QuickClinique.Services
{
    /// <summary>
    /// Helper class for timezone-aware date and time operations.
    /// Converts server time (UTC) to Philippine Time (UTC+8).
    /// </summary>
    public static class TimeZoneHelper
    {
        // Philippine Time is UTC+8 (no daylight saving time)
        private static readonly TimeSpan PhilippineTimeOffset = TimeSpan.FromHours(8);
        
        /// <summary>
        /// Gets the current date and time in Philippine Time (UTC+8).
        /// </summary>
        public static DateTime GetPhilippineTime()
        {
            return DateTime.UtcNow.Add(PhilippineTimeOffset);
        }
        
        /// <summary>
        /// Gets the current date in Philippine Time.
        /// </summary>
        public static DateOnly GetPhilippineDate()
        {
            return DateOnly.FromDateTime(GetPhilippineTime());
        }
        
        /// <summary>
        /// Gets the current time in Philippine Time.
        /// </summary>
        public static TimeOnly GetPhilippineTimeOnly()
        {
            return TimeOnly.FromDateTime(GetPhilippineTime());
        }
        
        /// <summary>
        /// Converts a UTC DateTime to Philippine Time.
        /// </summary>
        public static DateTime ToPhilippineTime(DateTime utcDateTime)
        {
            return utcDateTime.Add(PhilippineTimeOffset);
        }
        
        /// <summary>
        /// Converts a Philippine Time DateTime to UTC.
        /// </summary>
        public static DateTime ToUtc(DateTime philippineTime)
        {
            return philippineTime.Subtract(PhilippineTimeOffset);
        }
    }
}

