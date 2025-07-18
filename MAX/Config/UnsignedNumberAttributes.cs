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


namespace MAX.Config
{
    public class ConfigUnsignedIntegerAttribute : ConfigAttribute
    {
        public ConfigUnsignedIntegerAttribute(string name, string section) : base(name, section)
        {
        }

        // separate function to avoid boxing in derived classes
        // Use ulong instead of uint to allow larger inputs
        public ulong ParseUnsignedLong(string raw, ulong def, ulong min, ulong max)
        {
            if (!ulong.TryParse(raw, out ulong value))
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" has invalid unsigned integer '{2}', using default of {1}", Name, def, raw);
                value = def;
            }

            if (value < min)
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too small an unsigned integer, using {1}", Name, min);
                value = min;
            }
            if (value > max)
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too big an unsigned integer, using {1}", Name, max);
                value = max;
            }
            return value;
        }
        public uint ParseUnsignedInteger(string raw, uint def, uint min, uint max)
        {
            if (!uint.TryParse(raw, out uint value))
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" has invalid unsigned integer '{2}', using default of {1}", Name, def, raw);
                value = def;
            }

            if (value < min)
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too small an unsigned integer, using {1}", Name, min);
                value = min;
            }
            if (value > max)
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too big an unsigned integer, using {1}", Name, max);
                value = max;
            }
            return value;
        }
    }
    public class ConfigByteAttribute : ConfigUnsignedIntegerAttribute
    {
        public ConfigByteAttribute() : this(null, null)
        {
        }
        public ConfigByteAttribute(string name, string section) : base(name, section)
        {
        }

        public override object Parse(string raw)
        {
            return (byte)ParseUnsignedInteger(raw, 0, 0, byte.MaxValue);
        }
    }

    public class ConfigBlockAttribute : ConfigUnsignedIntegerAttribute
    {
        public ushort defBlock;
        public ConfigBlockAttribute() : this(null, null, Block.Air)
        {
        }
        public ConfigBlockAttribute(string name, string section, ushort def) : base(name, section)
        {
            defBlock = def;
        }

        public override object Parse(string raw)
        {
            ushort block = (ushort)ParseUnsignedInteger(raw, defBlock, 0, Block.SUPPORTED_COUNT - 1);
            if (block == Block.Invalid) return Block.Invalid;
            return Block.MapOldRaw(block);
        }
    }
    public class ConfigUShortAttribute : ConfigUnsignedIntegerAttribute
    {
        public ConfigUShortAttribute() : this(null, null)
        {
        }
        public ConfigUShortAttribute(string name, string section) : base(name, section)
        {
        }

        public override object Parse(string raw)
        {
            return (ushort)ParseUnsignedInteger(raw, 0, 0, ushort.MaxValue);
        }
    }
    public class ConfigUIntAttribute : ConfigUnsignedIntegerAttribute
    {
        public uint defValue, minValue, maxValue;

        public ConfigUIntAttribute() : this(null, null, 0, 0, uint.MaxValue)
        {
        }
        public ConfigUIntAttribute(string name, string section, uint def,
            uint min = uint.MinValue, uint max = uint.MaxValue) : base(name, section)
        {
            defValue = def;
            minValue = min;
            maxValue = max;
        }

        public override object Parse(string value)
        {
            return ParseUnsignedInteger(value, defValue, minValue, maxValue);
        }
    }
}
