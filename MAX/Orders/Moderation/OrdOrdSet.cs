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
namespace MAX.Orders.Moderation {
    public sealed class OrdOrdSet : ItemPermsOrd {
        public override string name { get { return "OrdSet"; } }
        public override string shortcut { get { return "SetOrd"; } }


        public override void Use(Player p, string message, OrderData data) {
            string[] args = message.SplitSpaces(3);
            if (args.Length < 2) { Help(p); return; }
            
            string ordName = args[0], ordArgs = "";
            Search(ref ordName, ref ordArgs);
            Order ord = Find(ordName);
            
            if (ord == null) { p.Message("Could not find order entered"); return; }
            
            if (!p.CanUse(ord)) {
                ord.Permissions.MessageCannotUse(p);
                p.Message("Therefore you cannot change the permissions of &T/{0}", ord.name); return;
            }
            
            if (args.Length == 2) {
                SetPerms(p, args, data, ord.Permissions, "order");
            } else {
                int num = 0;
                if (!OrderParser.GetInt(p, args[2], "Extra permission number", ref num)) return;
                
                OrderExtraPerms perms = OrderExtraPerms.Find(ord.name, num);
                if (perms == null) {
                    p.Message("This order has no extra permission by that number."); return;
                }
                SetPerms(p, args, data, perms, "extra permission");
            }
        }

        public override void UpdatePerms(ItemPerms perms, Player p, string msg) {
            if (perms is OrderPerms) {
                OrderPerms.Save();
                OrderPerms.ApplyChanges();
                Announce(p, perms.ItemName + msg);
            } else {
                OrderExtraPerms.Save();
                OrderExtraPerms ex = (OrderExtraPerms)perms;
                //Announce(p, ord.name + "&S's extra permission " + idx + " was set to " + grp.ColoredName);
                Announce(p, ex.OrdName + " extra permission #" + ex.Num + msg);
            }
        }
        
        public override void Help(Player p) {
            p.Message("&T/OrdSet [ord] [rank]");
            p.Message("&HSets lowest rank that can use [ord] to [rank]");
            p.Message("&T/OrdSet [ord] +[rank]");
            p.Message("&HAllows a specific rank to use [ord]");
            p.Message("&T/OrdSet [ord] -[rank]");
            p.Message("&HPrevents a specific rank from using [ord]");
            p.Message("&T/OrdSet [ord] [rank] [extra permission number]");
            p.Message("&HSet the lowest rank that has that extra permission for [ord]");
            p.Message("&HTo see available ranks, type &T/ViewRanks");
        }
    }
}