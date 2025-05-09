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
using MAX.Games;
namespace MAX.Orders.World {
    public sealed class OrdReload : Order2 {
        public override string name { get { return "Reload"; } }
        public override string shortcut { get { return "Reveal"; } }
        public override string type { get { return OrderTypes.World; } }
        public override bool museumUsable { get { return false; } }
        public override OrderDesignation[] Designations {
            get { return new [] { new OrderDesignation("ReJoin"), new OrderDesignation("rd"),
                    new OrderDesignation("WFlush"), new OrderDesignation("WorldFlush") }; }
        }
        public override OrderPerm[] ExtraPerms {
            get { return new[] { new OrderPerm(LevelPermission.Operator, "can reload for all players") }; }
        }

        public override void Use(Player p, string message, OrderData data) {
            if (CheckSuper(p, message, "level name")) return;
            
            if (message.Length == 0) {
                if (!IGame.CheckAllowed(p, "use &T/Reload")) {
                    // messaging handled in CheckAllowed
                } else if (!Hacks.CanUseNoclip(p)) {
                    p.Message("You cannot use &T/Reload &Son this level");
                } else {
                    PlayerActions.ReloadMap(p);
                    p.Message("&bMap reloaded");
                }
                return;
            } 
            
            if (!CheckExtraPerm(p, data, 1)) return;
            Level lvl = p.level;
            
            if (!message.CaselessEq("all")) {
                lvl = Matcher.FindLevels(p, message);
                if (lvl == null) return;
            }
            LevelActions.ReloadAll(lvl, p, true);
        }
        
        public override void Help(Player p) {
            p.Message("&T/Reload &H- Reloads the level you are in, just for you");
            p.Message("&T/Reload all &H- Reloads for all players in level you are in");
            p.Message("&T/Reload [level] &H- Reloads for all players in [level]");
        }
    }
}