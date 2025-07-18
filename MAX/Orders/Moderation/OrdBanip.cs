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
using System.Collections.Generic;
using System.Net;

namespace MAX.Orders.Moderation
{
    public class OrdBanip : Order
    {
        public override string Name { get { return "BanIP"; } }
        public override string Shortcut { get { return "bi"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("IPBan") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(2);
            string addr = ModActionOrd.FindIP(p, args[0], "BanIP", out string _);
            if (addr == null) return;

            if (!IPAddress.TryParse(addr, out IPAddress ip)) { p.Message("\"{0}\" is not a valid IP.", addr); return; }
            if (IPAddress.IsLoopback(ip)) { p.Message("You cannot IP ban the server."); return; }
            if (ip.Equals(p.IP)) { p.Message("You cannot IP ban yourself."); return; }
            if (Server.bannedIP.Contains(addr)) { p.Message("{0} is already IP banned.", addr); return; }

            // Check if IP is shared by any other higher ranked accounts
            if (!CheckIP(p, data, addr)) return;

            string reason = args.Length > 1 ? args[1] : "";
            reason = ModActionOrd.ExpandReason(p, reason);
            if (reason == null) return;

            ModAction action = new ModAction(addr, p, ModActionType.BanIP, reason);
            OnModActionEvent.Call(action);
        }

        public static bool CheckIP(Player p, OrderData data, string ip)
        {
            if (p.IsMAX) return true;
            List<string> accounts = PlayerInfo.FindAccounts(ip);

            if (accounts == null || accounts.Count == 0) return true;
            if (!Server.Config.ProtectStaffIPs) return true;

            foreach (string name in accounts)
            {
                Group grp = PlayerInfo.GetGroup(name);
                if (grp.Permission < data.Rank) continue;

                p.Message("You can only IP ban IPs used by players with a lower rank.");
                p.Message(name + "(" + grp.ColoredName + "&S) uses that IP.");

                Logger.Log(LogType.SuspiciousActivity,
                           "{0} failed to ipban {1} - IP is also used by: {2}({3})", p.name, ip, name, grp.Name);
                return false;
            }
            return true;
        }

        public override void Help(Player p)
        {
            p.Message("&T/BanIP [ip/player] <reason>");
            p.Message("&HBans an IP, or the IP the given player is on.");
            p.Message("&HFor <reason>, @number can be used as a shortcut for that rule.");
        }
    }
}