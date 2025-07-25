﻿/*
    Copyright 2015-2024 MCGalaxy
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    https://opensource.org/license/ecl-2-0/
    https://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System.Globalization;
namespace MAX
{
    public static class NumberUtils
    {
        public const NumberStyles DECIMAL_STYLE = NumberStyles.Integer | NumberStyles.AllowDecimalPoint;
        public const NumberStyles INTEGER_STYLE = NumberStyles.Integer;
        public static bool TryParseSingle(string s, out float result)
        {
            if (s != null && s.IndexOf(',') >= 0)
            {
                s = s.Replace(',', '.');
            }
            result = 0;
            if (!float.TryParse(s, DECIMAL_STYLE, NumberFormatInfo.InvariantInfo, out float temp))
            {
                return false;
            }
            if (float.IsInfinity(temp) || float.IsNaN(temp))
            {
                return false;
            }
            result = temp;
            return true;
        }
        public static bool TryParseDouble(string s, out double result)
        {
            if (s != null && s.IndexOf(',') >= 0)
            {
                s = s.Replace(',', '.');
            }
            result = 0;
            if (!double.TryParse(s, DECIMAL_STYLE, NumberFormatInfo.InvariantInfo, out double temp))
            {
                return false;
            }
            if (double.IsInfinity(temp) || double.IsNaN(temp))
            {
                return false;
            }
            result = temp;
            return true;
        }
        public static string StringifyDouble(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        public static string StringifyInt(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        public static bool TryParseInt32(string s, out int result)
        {
            return int.TryParse(s, INTEGER_STYLE, NumberFormatInfo.InvariantInfo, out result);
        }
        public static int ParseInt32(string s)
        {
            return int.Parse(s, INTEGER_STYLE, NumberFormatInfo.InvariantInfo);
        }
        public static bool TryParseInt8(string s, out sbyte result)
        {
            return sbyte.TryParse(s, INTEGER_STYLE, NumberFormatInfo.InvariantInfo, out result);
        }
    }
}