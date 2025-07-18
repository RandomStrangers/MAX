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
namespace MAX.Orders.Chatting
{
    public class OrdOpChat : Order
    {
        public override string Name { get { return "OpChat"; } }
        public override string Shortcut { get { return "Op"; } }
        public override string Type { get { return OrderTypes.Chat; } }
        public override bool UseableWhenJailed { get { return true; } }
        public override bool UpdatesLastOrd { get { return false; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Operator, "can read opchat messages") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length > 0) { ChatModes.MessageOps(p, message); return; }

            p.opchat = !p.opchat;
            if (p.opchat) p.Message("All messages will now be sent to OPs only");
            else p.Message("OP chat turned off");
        }

        public override void Help(Player p)
        {
            p.Message("&T/OpChat [message]");
            p.Message("&HSends a message to online OPs");
            p.Message("&T/OpChat");
            p.Message("&HMakes all messages sent go to OPs by default");
        }
    }
}