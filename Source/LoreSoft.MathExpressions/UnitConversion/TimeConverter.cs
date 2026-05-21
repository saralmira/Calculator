using System;
using LoreSoft.MathExpressions.Metadata;

namespace LoreSoft.MathExpressions.UnitConversion
{
    /// <summary>Units for Time</summary>
    public enum TimeUnit
    {
        /// <summary>Millisecond unit (ms)</summary>
        [Abbreviation("ms")]
        Millisecond = 0,
        /// <summary>Second unit (sec)</summary>
        [Abbreviation("sec")]
        Second = 1,
        /// <summary>Minute unit (min)</summary>
        [Abbreviation("min")]
        Minute = 2,
        /// <summary>Hour unit (hr)</summary>
        [Abbreviation("hr")]
        Hour = 3,
        /// <summary>Day unit (d)</summary>
        [Abbreviation("d")]
        Day = 4,
        /// <summary>Week unit (wk)</summary>
        [Abbreviation("wk")]
        Week = 5
    }

    /// <summary>
    /// Class representing time convertion.
    /// </summary>
    public static class TimeConverter
    {

        /// <summary>
        /// Converts the specified from unit to the specified unit.
        /// </summary>
        /// <param name="fromUnit">Covert from unit.</param>
        /// <param name="toUnit">Covert to unit.</param>
        /// <param name="fromValue">Covert from value.</param>
        /// <returns>The converted value.</returns>
        public static decimal Convert(
            TimeUnit fromUnit,
            TimeUnit toUnit,
            decimal fromValue)
        {
            if (fromUnit == toUnit)
                return fromValue; 
            
            TimeSpan span;
            switch (fromUnit)
            {
                case TimeUnit.Millisecond:
                    span = TimeSpan.FromMilliseconds((double)fromValue);
                    break;
                case TimeUnit.Second:
                    span = TimeSpan.FromSeconds((double)fromValue);
                    break;
                case TimeUnit.Minute:
                    span = TimeSpan.FromMinutes((double)fromValue);
                    break;
                case TimeUnit.Hour:
                    span = TimeSpan.FromHours((double)fromValue);
                    break;
                case TimeUnit.Day:
                    span = TimeSpan.FromDays((double)fromValue);
                    break;
                case TimeUnit.Week:
                    span = TimeSpan.FromDays((double)(fromValue * (decimal)7d));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fromUnit");
            }

            switch (toUnit)
            {
                case TimeUnit.Millisecond:
                    return (decimal)span.TotalMilliseconds;
                case TimeUnit.Second:
                    return (decimal)span.TotalSeconds;
                case TimeUnit.Minute:
                    return (decimal)span.TotalMinutes;
                case TimeUnit.Hour:
                    return (decimal)span.TotalHours;
                case TimeUnit.Day:
                    return (decimal)span.TotalDays;
                case TimeUnit.Week:
                    return (decimal)(span.TotalDays / 7d);
                default:
                    throw new ArgumentOutOfRangeException("toUnit");
            }
        }
    }
}
