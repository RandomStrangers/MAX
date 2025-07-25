﻿/*
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
namespace MAX.Orders.Chatting
{
    public class OrdSay : Order
    {
        public override string Name { get { return "Say"; } }
        public override string Shortcut { get { return "Broadcast"; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }

            message = Colors.Escape(message);
            Chat.Message(ChatScope.Global, message, null, null, true);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Say [message]");
            p.Message("&HBroadcasts a global message to everyone in the server.");
        }
    }
}