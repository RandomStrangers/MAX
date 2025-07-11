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
    public class OrdModerate : Order
    {
        public override string Name { get { return "Moderate"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length > 0) { Help(p); return; }

            if (Server.chatmod)
            {
                Chat.MessageAll("Chat moderation has been disabled. Everyone can now speak.");
            }
            else
            {
                Chat.MessageAll("Chat moderation engaged! Silence the plebians!");
            }
            Server.chatmod = !Server.chatmod;
        }

        public override void Help(Player p)
        {
            p.Message("&T/Moderate &H- Toggles chat moderation status.");
            p.Message("&HWhen enabled, only players with &T/Voice &Hmay speak.");
        }
    }
}