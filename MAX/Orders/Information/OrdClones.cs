/*
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
using MAX.Orders.Moderation;
using System.Collections.Generic;

namespace MAX.Orders.Info
{
    public class OrdClones : Order
    {
        public override string Name { get { return "Clones"; } }
        public override string Shortcut { get { return "Alts"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Owner; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("WhoIP") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0)
            {
                if (p.IsSuper) { SuperRequiresArgs(p, "IP address"); return; }
                message = p.ip;
            }
            else
            {
                message = ModActionOrd.FindIP(p, message, "Clones", out string name);
                if (message == null) return;
            }

            List<string> accounts = PlayerInfo.FindAccounts(message);
            if (accounts.Count == 0)
            {
                p.Message("No players last played with the given IP.");
            }
            else
            {
                p.Message("These players have the same IP:");
                p.Message(accounts.Join(alt => p.FormatNick(alt)));
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Clones [name]");
            p.Message("&HFinds everyone with the same IP as [name]");
            p.Message("&T/Clones [ip address]");
            p.Message("&HFinds everyone who last played or is playing on the given IP");
        }
    }
}