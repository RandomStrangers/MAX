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
using System.Collections.Generic;

namespace MAX.Orders.CPE
{
    public class OrdCustomColors : Order
    {
        public override string Name { get { return "CustomColors"; } }
        public override string Shortcut { get { return "ccols"; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces();
            if (message.Length == 0) { Help(p); return; }
            string ord = args[0];

            if (IsCreateOrder(ord))
            {
                AddHandler(p, args);
            }
            else if (IsDeleteOrder(ord))
            {
                RemoveHandler(p, args);
            }
            else if (IsEditOrder(ord))
            {
                EditHandler(p, args);
            }
            else if (IsListOrder(ord))
            {
                string modifer = args.Length > 1 ? args[1] : "";
                ListHandler(p, "ccols list", modifer);
            }
            else
            {
                Help(p);
            }
        }

        public void AddHandler(Player p, string[] args)
        {
            if (args.Length <= 4) { Help(p); return; }
            char code = args[1][0];
            if (code >= 'A' && code <= 'F') code += ' ';

            if (code == ' ' || code == '\0' || code == '\u00a0' || code == '%' || code == '&')
            {
                p.Message("&WColor code cannot be a space, percentage, or ampersand.");
                return;
            }
            if (Colors.IsSystem(code))
            {
                p.Message("&WCannot change system defined color codes using %T/CustomColors");
                return;
            }

            if (!CheckName(p, args[2]) || !CheckFallback(p, args[3], code, out char fallback)) return;
            ColorDesc col = default;
            if (!OrderParser.GetHex(p, args[4], ref col)) return;

            col.Code = code; col.Fallback = fallback; col.Name = args[2];
            Colors.Update(col);
            p.Message("Successfully added '{0}' color", code);
        }

        public void RemoveHandler(Player p, string[] args)
        {
            if (args.Length < 2) { Help(p); return; }

            char code = ParseColor(p, args[1]);
            if (code == '\0') return;

            Colors.Update(Colors.DefaultCol(code));
            p.Message("Successfully removed '{0}' color", code);
        }

        public static void ListHandler(Player p, string ord, string modifier)
        {
            List<ColorDesc> validColors = new List<ColorDesc>(Colors.List.Length);
            foreach (ColorDesc color in Colors.List)
            {
                if (color.IsModified()) validColors.Add(color);
            }

            Paginator.Output(p, validColors, PrintColor,
                             ord, "Colors", modifier);
        }

        // Not very elegant, because we don't want the % to be escaped like everywhere else
        public static void PrintColor(Player p, ColorDesc col)
        {
            string format = "{0} &{1}({2})&S - %&S{1}, falls back to &{3}%&{3}{3}";
            if (col.Code == col.Fallback) format = "{0} &{1}({2})&S - %&S{1}";

            p.Message(format, col.Name, col.Code, Utils.Hex(col.R, col.G, col.B), col.Fallback);
        }

        public void EditHandler(Player p, string[] args)
        {
            if (args.Length < 4) { Help(p); return; }

            char code = ParseColor(p, args[1]);
            if (code == '\0') return;
            ColorDesc col = Colors.Get(code);

            if (args[2].CaselessEq("name"))
            {
                if (!CheckName(p, args[3])) return;

                p.Message("Set name of {0} to {1}", col.Name, args[3]);
                col.Name = args[3];
            }
            else if (args[2].CaselessEq("fallback"))
            {
                if (!CheckFallback(p, args[3], code, out char fallback)) return;

                p.Message("Set fallback of {0} to %&S{1}", col.Name, fallback);
                col.Fallback = fallback;
            }
            else if (args[2].CaselessEq("hex") || args[2].CaselessEq("color"))
            {
                ColorDesc rgb = default;
                if (!OrderParser.GetHex(p, args[3], ref rgb)) return;

                p.Message("Set hex color of {0} to {1}", col.Name, Utils.Hex(rgb.R, rgb.G, rgb.B));
                col.R = rgb.R; col.G = rgb.G; col.B = rgb.B;
            }
            else
            {
                Help(p); return;
            }

            Colors.Update(col);
        }


        public static bool CheckName(Player p, string arg)
        {
            if (Colors.Parse(arg).Length > 0)
            {
                p.Message("There is already an existing color named \"{0}\".", arg);
                return false;
            }
            return true;
        }

        public static char ParseColor(Player p, string arg)
        {
            if (arg.Length != 1)
            {
                string colCode = Matcher.FindColor(p, arg);
                if (colCode != null) return colCode[1];
            }
            else
            {
                char code = arg[0];
                if (Colors.IsDefined(code)) return code;

                p.Message("There is no color with the code {0}.", code);
                p.Message("Use &T/CustomColors list &Sto see a list of colors.");
            }
            return '\0';
        }

        public static bool CheckFallback(Player p, string arg, char code, out char fallback)
        {
            fallback = arg[0];
            if (!Colors.IsStandard(fallback))
            {
                p.Message("{0} must be a standard color code.", fallback); return false;
            }
            // Can't change fallback of standard colour code
            if (Colors.IsStandard(code)) fallback = code;

            if (fallback >= 'A' && fallback <= 'F') fallback += ' ';
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("&T/CustomColors add [code] [name] [fallback] [hex]");
            p.Message("&H  code is a single character.");
            p.Message("&H  fallback is the color code shown to non-supporting clients.");
            p.Message("&T/CustomColors remove [code] &H- Removes that custom color.");
            p.Message("&T/CustomColors list [offset] &H- lists all custom colors.");
            p.Message("&T/CustomColors edit [code] [name/fallback/hex] [value]");
        }
    }
}