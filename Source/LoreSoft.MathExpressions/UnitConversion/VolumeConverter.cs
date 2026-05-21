using LoreSoft.MathExpressions.Metadata;
using System.ComponentModel;
namespace LoreSoft.MathExpressions.UnitConversion
{
    /// <summary>Units for Liquid Volume</summary>
    public enum VolumeUnit
    {
        /// <summary>Milliliter unit (ml)</summary>
        [Abbreviation("ml")]
        Milliliter = 0,
        /// <summary>Liter unit (l)</summary>
        [Abbreviation("l")]
        Liter = 1,
        /// <summary>Kiloliter unit (kl)</summary>
        [Abbreviation("kl")]
        Kiloliter = 2,
        /// <summary>Fluid ounce unit (oz)</summary>
        [Abbreviation("oz")]
        [Description("Fluid Ounce")]
        FluidOunce = 3,
        /// <summary>Cup unit (cup)</summary>
        [Abbreviation("cup")]
        Cup = 4,
        /// <summary>Pint unit (pt)</summary>
        [Abbreviation("pt")]
        Pint = 5,
        /// <summary>Quart unit (qt)</summary>
        [Abbreviation("qt")]
        Quart = 6,
        /// <summary>Gallon unit (gal)</summary>
        [Abbreviation("gal")]
        Gallon = 7
    }

    /// <summary>
    /// Class representing liquid volume convertion.
    /// </summary>
    public static class VolumeConverter
    {
        // In enum order
        private static readonly decimal[] factors = new decimal[]
            {
                (decimal)0.000001d,  			//milliliter
                (decimal)0.001d,  			    //liter
                (decimal)1d,  				    //kiloliter
                (decimal)(0.0037854118d/128d), 	//ounce [US, liquid]
                (decimal)(0.0037854118d/16d),  	//cup [US]
                (decimal)(0.0037854118d/8d),  	    //pint [US, liquid]
                (decimal)(0.0037854118d/4d),  	    //quart [US, liquid]
                (decimal)0.0037854118d,  		//gallon [US, liquid]
            };

        /// <summary>
        /// Converts the specified from unit to the specified unit.
        /// </summary>
        /// <param name="fromUnit">Covert from unit.</param>
        /// <param name="toUnit">Covert to unit.</param>
        /// <param name="fromValue">Covert from value.</param>
        /// <returns>The converted value.</returns>
        public static decimal Convert(
            VolumeUnit fromUnit,
            VolumeUnit toUnit,
            decimal fromValue)
        {
            if (fromUnit == toUnit)
                return fromValue;

            var fromFactor = factors[(int)fromUnit];
            var toFactor = factors[(int)toUnit];
            var result = fromFactor * fromValue / toFactor;
            return result;
        }
    }
}
