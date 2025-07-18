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
using MAX.DB;
using System;

namespace MAX.Orders.Info
{
    public class OrdSeen : Order
    {
        public override string Name { get { return "Seen"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0)
            {
                if (p.IsSuper) { SuperRequiresArgs(p, "player name"); return; }
                message = p.name;
            }
            if (!Formatter.ValidPlayerName(p, message)) return;

            Player pl = PlayerInfo.FindMatches(p, message, out int matches);
            if (matches > 1) return;
            if (matches == 1)
            {
                Show(p, pl.ColoredName, pl.FirstLogin, pl.LastLogin);
                p.Message("{0} &Sis currently online.", p.FormatNick(pl));
                return;
            }

            p.Message("Searching PlayerDB..");
            PlayerData target = PlayerDB.Match(p, message);
            if (target == null) return;
            Show(p, target.Name, target.FirstLogin, target.LastLogin);
        }

        public static void Show(Player p, string name, DateTime first, DateTime last)
        {
            TimeSpan firstDelta = DateTime.Now - first;
            TimeSpan lastDelta = DateTime.Now - last;

            name = p.FormatNick(name);
            p.Message("{0} &Swas first seen at {1:H:mm} on {1:yyyy-MM-dd} ({2} ago)", name, first, firstDelta.Shorten());
            p.Message("{0} &Swas last seen at {1:H:mm} on {1:yyyy-MM-dd} ({2} ago)", name, last, lastDelta.Shorten());
        }

        public override void Help(Player p)
        {
            p.Message("&T/Seen [player]");
            p.Message("&HSays when a player was first and last seen on the server");
        }
    }
}