﻿/*
    Copyright 2015 MCGalaxy
        
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
using System;
using BlockID = System.UInt16;

namespace MAX.Config 
{    
    public abstract class ConfigIntegerAttribute : ConfigAttribute 
    {
        public ConfigIntegerAttribute(string name, string section) 
            : base(name, section) { }

        // separate function to avoid boxing in derived classes
        public int ParseInteger(string raw, int def, int min, int max) {
            int value;
            if (!int.TryParse(raw, out value)) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" has invalid integer '{2}', using default of {1}", Name, def, raw);
                value = def;
            }
            
            if (value < min) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too small an integer, using {1}", Name, min);
                value = min;
            }
            if (value > max) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too big an integer, using {1}", Name, max);
                value = max;
            }
            return value;
        }
    }

    public sealed class ConfigIntAttribute : ConfigIntegerAttribute 
    {
        public int defValue, minValue, maxValue;
        
        public ConfigIntAttribute()
            : this(null, null, 0, int.MinValue, int.MaxValue) { }
        public ConfigIntAttribute(string name, string section, int def,
                                  int min = int.MinValue, int max = int.MaxValue)
            : base(name, section) { defValue = def; minValue = min; maxValue = max; }
        
        public override object Parse(string value) {
            return ParseInteger(value, defValue, minValue, maxValue);
        }
    }
    public abstract class ConfigNumeralByteAttribute : ConfigAttribute
    {
        public ConfigNumeralByteAttribute(string name, string section)
            : base(name, section) { }

        // separate function to avoid boxing in derived classes
        public byte ParseNumeralByte(string raw, byte def, byte min, byte max)
        {
            byte value;
            if (!byte.TryParse(raw, out value))
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" has invalid byte '{2}', using default of {1}", Name, def, raw);
                value = def;
            }

            if (value < min)
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too small a byte, using {1}", Name, min);
                value = min;
            }
            if (value > max)
            {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too big a byte, using {1}", Name, max);
                value = max;
            }
            return value;
        }
    }

    public sealed class ConfigNByteAttribute : ConfigNumeralByteAttribute
    {
        public byte defValue, minValue, maxValue;

        public ConfigNByteAttribute()
            : this(null, null, 0, byte.MinValue, byte.MaxValue) { }
        public ConfigNByteAttribute(string name, string section, byte def,
                                  byte min = byte.MinValue, byte max = byte.MaxValue)
            : base(name, section) { defValue = def; minValue = min; maxValue = max; }

        public override object Parse(string value)
        {
            return ParseNumeralByte(value, defValue, minValue, maxValue);
        }
    }
    public sealed class ConfigBlockAttribute : ConfigIntegerAttribute 
    {
        public BlockID defBlock;
        public ConfigBlockAttribute() : this(null, null, Block.Air) { }
        public ConfigBlockAttribute(string name, string section, BlockID def)
            : base(name, section) { defBlock = def; }
        
        public override object Parse(string raw) {
            BlockID block = (BlockID)ParseInteger(raw, defBlock, 0, Block.SUPPORTED_COUNT - 1);
            if (block == Block.Invalid) return Block.Invalid;
            return Block.MapOldRaw(block);
        }
    }
    
    public class ConfigByteAttribute : ConfigIntegerAttribute 
    {
        public ConfigByteAttribute() : this(null, null) { }
        public ConfigByteAttribute(string name, string section) : base(name, section) { }
        
        public override object Parse(string raw) { 
            return (byte)ParseInteger(raw, 0, 0, byte.MaxValue); 
        }
    }
    
    public class ConfigUShortAttribute : ConfigIntegerAttribute 
    {
        public ConfigUShortAttribute() : this(null, null) { }
        public ConfigUShortAttribute(string name, string section) : base(name, section) { }
        
        public override object Parse(string raw) { 
            return (ushort)ParseInteger(raw, 0, 0, ushort.MaxValue);
        }
    }
    
    public abstract class ConfigRealAttribute : ConfigAttribute 
    {
        public ConfigRealAttribute(string name, string section) 
            : base(name, section) { }

        public double ParseReal(string raw, double def, double min, double max) {
            double value;
            if (!Utils.TryParseDouble(raw, out value)) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" has invalid number '{2}', using default of {1}", Name, def, raw);
                value = def;
            }
            
            if (value < min) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too small a number, using {1}", Name, min);
                value = min;
            }
            if (value > max) {
                Logger.Log(LogType.Warning, "Config key \"{0}\" is too big a number, using {1}", Name, max);
                value = max;
            }
            return value;
        }

        public override string Serialise(object value) {
            if (value is float)  return Utils.StringifyDouble((float)value);
            if (value is double) return Utils.StringifyDouble((double)value);
            return base.Serialise(value);
        }
    }
    
    public class ConfigFloatAttribute : ConfigRealAttribute 
    {
        public float defValue, minValue, maxValue;
        
        public ConfigFloatAttribute()
            : this(null, null, 0, float.NegativeInfinity, float.PositiveInfinity) { }
        public ConfigFloatAttribute(string name, string section, float def,
                                    float min = float.NegativeInfinity, float max = float.PositiveInfinity)
            : base(name, section) { defValue = def; minValue = min; maxValue = max; }
        
        public override object Parse(string raw) {
            return (float)ParseReal(raw, defValue, minValue, maxValue);
        }
    }
    
    public class ConfigTimespanAttribute : ConfigRealAttribute 
    {
        public bool mins; int def;
        public ConfigTimespanAttribute(string name, string section, int def, bool mins)
            : base(name, section) { this.def = def; this.mins = mins; }
        
        public override object Parse(string raw) {
            double value = ParseReal(raw, def, 0, int.MaxValue);
            return ParseInput(value);
        }

        public TimeSpan ParseInput(double value) {
            if (mins) {
                return TimeSpan.FromMinutes(value);
            } else {
                return TimeSpan.FromSeconds(value);
            }
        }
        
        public override string Serialise(object value) {
            TimeSpan span = (TimeSpan)value;
            double time = mins ? span.TotalMinutes : span.TotalSeconds;
            return time.ToString();
        }
    }
    
    public class ConfigOptTimespanAttribute : ConfigTimespanAttribute 
    {
        public ConfigOptTimespanAttribute(string name, string section, bool mins)
            : base(name, section, -1, mins) { }
        
        public override object Parse(string raw) {
            if (string.IsNullOrEmpty(raw)) return null;
        	
            double value = ParseReal(raw, -1, -1, int.MaxValue);
            if (value < 0) return null;
            
            return ParseInput(value);
        }
        
        public override string Serialise(object value) {
            if (value == null) return "";
            
            return base.Serialise(value);
        }
    }
}