/*
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

namespace MAX.Orders.Chatting
{
    public class OrdClear : Order
    {
        public override string Name { get { return "Clear"; } }
        public override string Shortcut { get { return "cls"; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("PlayerCLS"), new OrderDesignation("GlobalCLS", "global"), new OrderDesignation("gcls", "global") }; }
        }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Admin, "can clear chat for everyone") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (!message.CaselessEq("global"))
            {
                ClearChat(p);
                p.Message("&4Chat cleared.");
            }
            else
            {
                if (!CheckExtraPerm(p, data, 1)) return;

                Player[] players = PlayerInfo.Online.Items;
                foreach (Player pl in players)
                {
                    ClearChat(pl);
                }
                Chat.MessageAll("&4Global Chat cleared.");
            }
        }

        public static void ClearChat(Player p)
        {
            for (int i = 0; i < 30; i++)
            {
                p.Session.SendMessage(CpeMessageType.Normal, "");
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/Clear &H- Clears your chat.");
            p.Message("&T/Clear global &H- Clears chat of all users.");
        }
    }
}