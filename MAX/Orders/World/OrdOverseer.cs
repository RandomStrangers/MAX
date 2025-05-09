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

namespace MAX.Orders.World {
    public sealed partial class OrdOverseer : Order2 {
        public override string name { get { return "Overseer"; } }
        public override string shortcut { get { return Overseer.orderShortcut; } }
        public override string type { get { return OrderTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Builder; } }
        public override bool SuperUseable { get { return false; } }
        public override OrderDesignation[] Designations {
            get { return new[] { new OrderDesignation("Realm"), new OrderDesignation("MyRealm") }; }
        }
        public override OrderParallelism Parallelism { get { return OrderParallelism.NoAndWarn; } }
        
        public override void Use(Player p, string message, OrderData data) {
            if (message.Length == 0) { Help(p); return; }
            Overseer.subOrderGroup.Use(p, message);
        }
        
        public override void Help(Player p, string message) {
            message = message.SplitSpaces()[0]; // only first argument
            Overseer.subOrderGroup.DisplayHelpFor(p, message);
        }
        
        public override void Help(Player p) {
            p.Message("&T/os [order] [args]");
            p.Message("&HAllows you to modify and manage your personal realms.");
            Overseer.subOrderGroup.DisplayAvailable(p);
        }

    }
}