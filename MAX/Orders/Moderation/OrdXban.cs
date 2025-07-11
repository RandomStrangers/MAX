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
namespace MAX.Orders.Moderation
{
    public class OrdXban : Order
    {
        public override string Name { get { return "XBan"; } }
        public override string Shortcut { get { return "BanX"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("UBan", "-noip") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            bool banIP = true;
            if (message.CaselessStarts("-noip "))
            {
                message = message.Substring("-noip ".Length);
                banIP = false;
            }
            if (message.Length == 0) { Help(p); return; }

            string name = message.SplitSpaces()[0];
            Find("UndoPlayer").Use(p, name + " all", data);
            if (banIP) Find("BanIP").Use(p, "@" + name, data);
            Find("Ban").Use(p, message, data);
        }

        public override void Help(Player p)
        {
            p.Message("&T/XBan [player] <reason>");
            p.Message("&HBans, IP bans, undoes, and kicks the given player.");
            p.Message("&T/UBan [player] <reason>");
            p.Message("&HSame as &T/XBan&H, but does not ban the IP.");
        }
    }
}