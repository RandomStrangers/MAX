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
namespace MAX.Orders.Misc
{
    public class OrdInvincible : Order
    {
        public override string Name { get { return "Invincible"; } }
        public override string Shortcut { get { return "Inv"; } }
        public override string Type { get { return OrderTypes.Other; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("GodMode") }; }
        }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Operator, "can toggle invinciblity of others") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            Player who = message.Length == 0 ? p : PlayerInfo.FindMatches(p, message);
            if (who == null) return;

            if (p != who && !CheckExtraPerm(p, data, 1)) return;
            if (!CheckRank(p, data, who, "toggle invincibility", true)) return;

            who.invincible = !who.invincible;
            ShowPlayerMessage(p, who);
        }

        public static void ShowPlayerMessage(Player p, Player target)
        {
            string msg = target.invincible ? "now invincible" : "no longer invincible";
            if (p == target) p.Message("You are {0}", msg);

            string globalMsg = target.invincible ? Server.Config.InvincibleMessage : "has stopped being invincible";
            if (Server.Config.ShowInvincibleMessage && !target.hidden)
            {
                Chat.MessageFrom(target, "λNICK &S" + globalMsg);
            }
            else if (p != target)
            {
                p.Message("{0} &Sis {1}.", p.FormatNick(target), msg);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Invincible <name>");
            p.Message("&HTurns invincible mode on/off.");
            p.Message("&HIf <name> is given, that player's invincibility is toggled");
        }
    }
}