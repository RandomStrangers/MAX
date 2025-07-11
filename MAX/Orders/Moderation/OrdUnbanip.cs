/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCForge)
    
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
using MAX.Events;
using System.Net;

namespace MAX.Orders.Moderation
{
    public class OrdUnbanip : Order
    {
        public override string Name { get { return "UnbanIP"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("UnIPBan") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(2);
            string addr = ModActionOrd.FindIP(p, args[0], "UnbanIP", out string _);
            if (addr == null) return;

            if (!IPAddress.TryParse(addr, out IPAddress ip)) { p.Message("\"{0}\" is not a valid IP.", addr); return; }
            if (ip.Equals(p.IP)) { p.Message("You cannot un-IP ban yourself."); return; }
            if (!Server.bannedIP.Contains(addr)) { p.Message(addr + " is not a banned IP."); return; }

            string reason = args.Length > 1 ? args[1] : "";
            reason = ModActionOrd.ExpandReason(p, reason);
            if (reason == null) return;

            ModAction action = new ModAction(addr, p, ModActionType.UnbanIP, reason);
            OnModActionEvent.Call(action);
        }

        public override void Help(Player p)
        {
            p.Message("&T/UnbanIP [ip/player]");
            p.Message("&HUn-bans an IP, or the IP the given player is on.");
        }
    }
}