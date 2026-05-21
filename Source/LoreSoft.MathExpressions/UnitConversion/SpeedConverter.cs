using LoreSoft.MathExpressions.Metadata;
using System;
using System.ComponentModel;
namespace LoreSoft.MathExpressions.UnitConversion
{
    /// <summary>Units for Speed</summary>
    public enum SpeedUnit
    {
        /// <summary>Meter/Second unit (m/s)</summary>
        [Abbreviation("m/s")]
        [Description("Meter/Second")]
        MeterPerSecond = 0,
        /// <summary>Kilometer/Hour unit (kph)</summary>
        [Abbreviation("kph")]
        [Description("Kilometer/Hour")]
        KilometerPerHour = 1,
        /// <summary>Foot/Second unit (ft/s)</summary>
        [Abbreviation("ft/s")]
        [Description("Foot/Second")]
        FootPerSecond = 2,
        /// <summary>Mile/Hour unit (mph)</summary>
        [Abbreviation("mph")]
        [Description("Mile/Hour")]
        MilePerHour = 3,
        /// <summary>Knot unit (knot)</summary>
        [Abbreviation("knot")]
        Knot = 4,
        /// <summary>Mach unit (mach)</summary>
        [Abbreviation("mach")]
        Mach = 5,
    }

    /// <summary>
    /// Class representing speed convertion.
    /// </summary>
    public static class SpeedConverter
    {
        // In enum order
        private static readonly decimal[] factors = new decimal[]
            {
                (decimal)1d,                         //meter/second
                (decimal)(1000d/3600d),                //kilometer/hour
                (decimal)(0.3048d),                    //foot/second
                (decimal)((0.3048d*5280d)/3600d),      //mile/hour (mph)
                (decimal)(1852d/3600d),                //knot
                (decimal)(340.29d),                    //mach
            };


        /// <summary>
        /// Converts the specified from unit to the specified unit.
        /// </summary>
        /// <param name="fromUnit">Covert from unit.</param>
        /// <param name="toUnit">Covert to unit.</param>
        /// <param name="fromValue">Covert from value.</param>
        /// <returns>The converted value.</returns>
        public static decimal Convert(
            SpeedUnit fromUnit,
            SpeedUnit toUnit,
            decimal fromValue)
        {
            if (fromUnit == toUnit)
                return fromValue;

            var fromFactor = (decimal)factors[(int)fromUnit];
            var toFactor = (decimal)factors[(int)toUnit];
            var result = fromFactor * fromValue / toFactor;
            return result;
        }
        
    }
}
