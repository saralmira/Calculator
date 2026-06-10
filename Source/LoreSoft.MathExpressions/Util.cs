using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoreSoft.MathExpressions
{
    public static class Util
    {
        public static decimal ParseFloatHexString(string hexString)
        {
            bool isDouble = hexString.Length > sizeof(float) * 2;

            unsafe
            {
                if (isDouble)
                {
                    var tmp = Convert.ToUInt64(hexString, 16);
                    return (decimal)*(double*)&tmp;
                }
                else
                {
                    var tmp = Convert.ToUInt32(hexString, 16);
                    return (decimal)*(float*)&tmp;
                }
            }
        }

        public static string ConvertToHexString(float value)
        {
            unsafe
            {
                UInt32 tmp;
                tmp = *(UInt32*)&value;
                return tmp.ToString("X");
            }
        }

        public static string ConvertToHexString(double value)
        {
            unsafe
            {
                UInt64 tmp;
                tmp = *(UInt64*)&value;
                return tmp.ToString("X");
            }
        }
    }
}
