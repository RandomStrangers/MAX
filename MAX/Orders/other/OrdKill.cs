﻿/*
    Copyright 2011 MCForge
        
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
using MAX.Orders.Chatting;

namespace MAX.Orders.Misc
{
    public class OrdKill : Order
    {
        public override string Name { get { return "Kill"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            if (!MessageOrd.CanSpeak(p, Name)) return; // do not allow using kill to spam every 2 secs

            string[] args = message.SplitSpaces(2);
            Player target = PlayerInfo.FindMatches(p, args[0]);

            if (target == null)
            {
                p?.HandleDeath(Block.Stone, "@p &Skilled " + p.Pronouns.Reflexive + " in " + p.Pronouns.Object + " confusion");
                return;
            }
            if (!CheckRank(p, data, target, "kill", false)) return;

            bool explode = false;
            string deathMsg = GetDeathMessage(args, p.ColoredName, ref explode);
            target.HandleDeath(Block.Stone, deathMsg, explode);
        }

        public static string GetDeathMessage(string[] args, string killer, ref bool explode)
        {
            if (args.Length < 2) return "@p &Swas killed by " + killer;

            if (args[1].CaselessEq("explode"))
            {
                explode = true;
                return "@p &Swas exploded by " + killer;
            }
            return "@p &S" + args[1];
        }

        public override void Help(Player p)
        {
            p.Message("&T/Kill [name] <message>");
            p.Message("&HKills [name], with <message> if given.");
            p.Message("&HCauses an explosion if \"explode\" is used for <message>");
        }
    }
}