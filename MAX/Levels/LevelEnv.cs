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
using MAX.Orders;
using System.Collections.Generic;
using System.Globalization;

namespace MAX
{
    public class EnvOption
    {
        public string Name, Help;
        public EnvOptions.OptionSetter SetFunc;

        public EnvOption(string name, EnvOptions.OptionSetter func, string help)
        {
            Name = name;
            SetFunc = func;
            Help = help;
        }
    }

    public static class EnvOptions
    {
        public delegate void OptionSetter(Player p, string area, EnvConfig cfg, string value);

        public static List<EnvOption> Options = new List<EnvOption>()
        {
             new EnvOption("Weather",   SetWeather,   "&HSets the weather (sun, rain, snow)"),
             new EnvOption("SmoothFog", SetSmoothFog, "&HSets whether smoother fog is used"),
             new EnvOption("Horizon",   SetHorizon,   "&HSets the \"ocean\" block outside the map"),
             new EnvOption("Border",    SetBorder,    "&HSets the \"bedrock\" block outside the map"),
             new EnvOption("CloudsHeight", SetCloudsHeight, "&HSets the clouds height of the map"),
             new EnvOption("EdgeLevel",    SetEdgeLevel,    "&HSets the water height of the map"),
             new EnvOption("SidesOffset",  SetSidesOffset,  "&HSets offset of bedrock from water (default -2)"),
             new EnvOption("MaxFog",       SetMaxFog,       "&HSets maximum fog distance in the map (e.g. 16 for a horror map)"),
             new EnvOption("Sky",       SetSky,       "&HSets color of the sky (default 99CCFF)"),
             new EnvOption("Clouds",    SetClouds,    "&HSets color of the clouds (default FFFFFF)"),
             new EnvOption("Fog",       SetFog,       "&HSets color of the fog (default FFFFFF)"),
             new EnvOption("Sun",       SetSun,       "&HSets color of blocks in sunlight (default FFFFFF)"),
             new EnvOption("Shadow",    SetShadow,    "&HSets color of blocks in darkness (default 9B9B9B)"),
             new EnvOption("Skybox",    SetSkybox,    "&HSets color of the skybox (default FFFFFF)"),
             new EnvOption("LavaLight", SetLavaLight, "&HSets color cast by bright natural blocks when fancy lighting is enabled (default FFEBC6)"),
             new EnvOption("LampLight", SetLampLight, "&HSets color cast by bright artificial blocks when fancy lighting is enabled (default FFFFFF)"),
             new EnvOption("CloudsSpeed",  SetCloudsSpeed,  "&HSets how fast clouds move (negative moves in opposite direction)"),
             new EnvOption("WeatherSpeed", SetWeatherSpeed, "&HSets how fast rain/snow falls (negative falls upwards)"),
             new EnvOption("WeatherFade",  SetWeatherFade,  "&HSets how quickly rain/snow fades out over distance"),
             new EnvOption("SkyboxHorSpeed", SetSkyboxHor,  "&HSets how many times per second skybox fully spins horizontally (e.g. 0.1 is once every 10 seconds)"),
             new EnvOption("SkyboxVerSpeed", SetSkyboxVer,  "&HSets how many times per second skybox fully spins vertically (e.g. 0.1 is once every 10 seconds)"),
        };

        public static EnvOption Find(string opt)
        {
            if (opt.CaselessEq("ExpFog")) opt = "SmoothFog";
            if (opt.CaselessEq("Edge")) opt = "Horizon";
            if (opt.CaselessEq("Side")) opt = "Border";
            if (opt.CaselessEq("Water")) opt = "Horizon";
            if (opt.CaselessEq("Bedrock")) opt = "Border";
            if (opt.CaselessEq("CloudHeight")) opt = "CloudsHeight";
            if (opt.CaselessEq("Level")) opt = "EdgeLevel";
            if (opt.CaselessEq("SideOffset")) opt = "SidesOffset";
            if (opt.CaselessEq("Bedrockffset")) opt = "SidesOffset";
            if (opt.CaselessEq("Cloud")) opt = "Clouds";
            if (opt.CaselessEq("Dark")) opt = "Shadow";
            if (opt.CaselessEq("Sunlight")) opt = "Sun";
            if (opt.CaselessEq("CloudSpeed")) opt = "CloudsSpeed";
            if (opt.CaselessEq("SkyboxHor")) opt = "SkyboxHorSpeed";
            if (opt.CaselessEq("SkyboxVer")) opt = "SkyboxVerSpeed";
            if (opt.CaselessEq("lavacolor")) opt = "LavaLight";
            if (opt.CaselessEq("lampcolor")) opt = "LampLight";
            if (opt.CaselessEq("lighting")) opt = "LightingMode";

            foreach (EnvOption option in Options)
            {
                if (option.Name.CaselessEq(opt)) return option;
            }
            return null;
        }


        public static void SetHorizon(Player p, string area, EnvConfig cfg, string value)
        {
            SetBlock(p, value, area, "edge block", ref cfg.HorizonBlock);
        }
        public static void SetBorder(Player p, string area, EnvConfig cfg, string value)
        {
            SetBlock(p, value, area, "sides block", ref cfg.EdgeBlock);
        }

        public static void SetCloudsHeight(Player p, string area, EnvConfig cfg, string value)
        {
            SetInt(p, value, area, "clouds height", ref cfg.CloudsHeight);
        }
        public static void SetEdgeLevel(Player p, string area, EnvConfig cfg, string value)
        {
            SetInt(p, value, area, "edge level", ref cfg.EdgeLevel);
        }
        public static void SetSidesOffset(Player p, string area, EnvConfig cfg, string value)
        {
            SetInt(p, value, area, "sides offset", ref cfg.SidesOffset);
        }
        public static void SetMaxFog(Player p, string area, EnvConfig cfg, string value)
        {
            SetInt(p, value, area, "max fog distance", ref cfg.MaxFogDistance);
        }

        public static void SetSky(Player p, string area, EnvConfig cfg, string value)
        {
            SetColor(p, value, area, "sky color", ref cfg.SkyColor);
        }
        public static void SetClouds(Player p, string area, EnvConfig cfg, string value)
        {
            SetColor(p, value, area, "cloud color", ref cfg.CloudColor);
        }
        public static void SetFog(Player p, string area, EnvConfig cfg, string value)
        {
            SetColor(p, value, area, "fog color", ref cfg.FogColor);
        }
        public static void SetSun(Player p, string area, EnvConfig cfg, string value)
        {
            SetColor(p, value, area, "sun color", ref cfg.LightColor);
        }
        public static void SetShadow(Player p, string area, EnvConfig cfg, string value)
        {
            SetColor(p, value, area, "shadow color", ref cfg.ShadowColor);
        }
        public static void SetSkybox(Player p, string area, EnvConfig cfg, string value)
        {
            SetColor(p, value, area, "skybox color", ref cfg.SkyboxColor);
        }
        public static void SetLavaLight(Player p, string area, EnvConfig cfg, string value)
        {
            SetColor(p, value, area, "block lava light color", ref cfg.LavaLightColor);
        }
        public static void SetLampLight(Player p, string area, EnvConfig cfg, string value)
        {
            SetColor(p, value, area, "block lamp light color", ref cfg.LampLightColor);
        }

        public static void SetCloudsSpeed(Player p, string area, EnvConfig cfg, string value)
        {
            SetFloat(p, value, area, 256, "clouds speed", ref cfg.CloudsSpeed, -0xFFFFFF, 0xFFFFFF);
        }
        public static void SetWeatherSpeed(Player p, string area, EnvConfig cfg, string value)
        {
            SetFloat(p, value, area, 256, "weather speed", ref cfg.WeatherSpeed, -0xFFFFFF, 0xFFFFFF);
        }
        public static void SetWeatherFade(Player p, string area, EnvConfig cfg, string value)
        {
            SetFloat(p, value, area, 128, "weather fade rate", ref cfg.WeatherFade, 0, 255);
        }
        public static void SetSkyboxHor(Player p, string area, EnvConfig cfg, string value)
        {
            SetFloat(p, value, area, 1024, "skybox horizontal speed", ref cfg.SkyboxHorSpeed, -0xFFFFFF, 0xFFFFFF);
        }
        public static void SetSkyboxVer(Player p, string area, EnvConfig cfg, string value)
        {
            SetFloat(p, value, area, 1024, "skybox vertical speed", ref cfg.SkyboxVerSpeed, -0xFFFFFF, 0xFFFFFF);
        }

        public static bool TryParseInt32(string s, out int result)
        {
            return int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out result);
        }
        public static bool IsResetString(string value)
        {
            return value.CaselessEq("normal") || value.CaselessEq("default")
                || value.CaselessEq("reset") || value.Length == 0;
        }

        public static void SetWeather(Player p, string area, EnvConfig cfg, string value)
        {
            int weather;
            if (IsResetString(value))
            {
                p.Message("Reset weather for {0} &Sto 0 (Sun)", area);
                weather = EnvConfig.ENV_USE_DEFAULT;
            }
            else
            {
                if (TryParseInt32(value, out weather))
                {
                }
                else if (value.CaselessEq("sun"))
                {
                    weather = 0;
                }
                else if (value.CaselessEq("rain"))
                {
                    weather = 1;
                }
                else if (value.CaselessEq("snow"))
                {
                    weather = 2;
                }

                if (weather < 0 || weather > 2)
                {
                    p.Message("Weather can be either sun, rain, or snow.");
                    return;
                }
                string type = weather == 0 ? "&SSun" : (weather == 1 ? "&1Rain" : "&fSnow");
                p.Message("Set weather for {0} &Sto {1} ({2}&S)", area, weather, type);
            }
            cfg.Weather = weather;
        }

        public static void SetSmoothFog(Player p, string area, EnvConfig cfg, string value)
        {
            if (IsResetString(value))
            {
                p.Message("Reset smooth fog for {0} &Sto &cOFF", area);
                cfg.ExpFog = EnvConfig.ENV_USE_DEFAULT;
            }
            else
            {
                bool enabled = false;
                if (!OrderParser.GetBool(p, value, ref enabled)) return;

                cfg.ExpFog = enabled ? 1 : 0;
                p.Message("Set smooth fog for {0} &Sto {1}", area, enabled ? "&aON" : "&cOFF");
            }
        }

        public static void SetBlock(Player p, string input, string area, string type, ref ushort target)
        {
            if (IsResetString(input))
            {
                p.Message("Reset {0} for {1} &Sto normal", type, area);
                target = Block.Invalid;
            }
            else
            {
                if (!OrderParser.GetBlock(p, input, out ushort block)) return;
                if (Block.IsPhysicsType(block))
                {
                    p.Message("&WCannot use physics block ids for &T/env");
                    return;
                }

                string name = Block.GetName(p, block);
                target = block;
                p.Message("Set {0} for {1} &Sto {2}", type, area, name);
            }
        }

        public static void SetInt(Player p, string input, string area, string type, ref int target)
        {
            if (IsResetString(input))
            {
                p.Message("Reset {0} for {1} &Sto normal", type, area);
                target = EnvConfig.ENV_USE_DEFAULT;
            }
            else
            {
                int value = 0;
                if (!OrderParser.GetInt(p, input, type, ref value,
                                          short.MinValue, short.MaxValue)) return;

                target = (short)value;
                p.Message("Set {0} for {1} &Sto {2}", type, area, value);
            }
        }

        public static void SetFloat(Player p, string input, string area, int scale, string type, ref int target, int min, int max)
        {
            if (IsResetString(input))
            {
                p.Message("Reset {0} for {1} &Sto normal", type, area);
                target = EnvConfig.ENV_USE_DEFAULT;
            }
            else
            {
                float value = 0, minF = (float)min / scale, maxF = (float)max / scale;
                if (!OrderParser.GetReal(p, input, type, ref value, minF, maxF)) return;

                target = (int)(value * scale);
                p.Message("Set {0} for {1} &Sto {2}", type, area, value.ToString("F4"));
            }
        }

        public static void SetColor(Player p, string input, string area, string variable, ref string target)
        {
            if (IsResetString(input))
            {
                p.Message("Reset {0} for {1} &Sto normal", variable, area);
                target = "";
            }
            else
            {
                ColorDesc rgb = default;
                if (!OrderParser.GetHex(p, input, ref rgb)) return;

                p.Message("Set {0} for {1} &Sto #{2}", variable, area, input);
                target = Utils.Hex(rgb.R, rgb.G, rgb.B);
            }
        }

        public static bool SetEnum<T>(Player p, string input, string area, string variable, T resetValue, ref T target) where T : struct
        {
            if (IsResetString(input))
            {
                p.Message("Reset {0} for {1} &Sto normal", variable, area);
                target = resetValue;
                return true;
            }
            else
            {
                if (!OrderParser.GetEnum(p, input, variable, ref target)) return false;
                p.Message("Set {0} for {1} &Sto {2}", variable, area, target.ToString());
                return true;
            }
        }
    }
}