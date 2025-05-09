/*
    Copyright 2011 MCForge
    
    Written by SebbiUltimate
        
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
namespace MAX.Orders.Misc {
    
    public sealed class OrdSendOrd : Order2 {        
        public override string name { get { return "SendOrd"; } }
        public override string type { get { return OrderTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }
        
        public override void Use(Player p, string message, OrderData data) {
            string[] args = message.SplitSpaces(3);
            Player target = PlayerInfo.FindMatches(p, args[0]);
            if (target == null) return;
            
            if (!CheckRank(p, data, target, "send orders for", true)) return;
            if (args.Length == 1) { p.Message("No order name given."); return; }
            
            string ordName = args[1], ordArgs = args.Length > 2 ? args[2] : "";
            Search(ref ordName, ref ordArgs);
            
            Order ord = Find(ordName);
            if (ord == null) {
                p.Message("Unknown order \"{0}\".", ordName); return;
            }
            
            data.Context = OrderContext.SendOrd;
            data.Rank = p.Rank;
            ord.Use(target, ordArgs, data);
        }

        public override void Help(Player p) {
            p.Message("&T/SendOrd [player] [order] <arguments>");
            p.Message("&HMake another user use a order. (e.g &T/SendOrd bob tp bob2&H)");
            p.Message("  &WNote [player] uses the order as if they had your rank");
        }
    }
}