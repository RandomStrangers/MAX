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

namespace MAX.Orders.World
{
    public class OrdDeleteLvl : Order
    {
        public override string Name { get { return "DeleteLvl"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("WDelete"), new OrderDesignation("WorldDelete"), new OrderDesignation("WRemove") }; }
        }
        public override bool MessageBlockRestricted { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0 || message.SplitSpaces().Length > 1) { Help(p); return; }
            string map = Matcher.FindMaps(p, message);

            if (map == null) return;
            if (!LevelInfo.Check(p, data.Rank, map, "delete this map", out LevelConfig cfg)) return;

            if (!LevelActions.Delete(p, map)) return;
            Chat.MessageGlobal("Level {0} &Swas deleted", cfg.Color + map);
        }

        public override void Help(Player p)
        {
            p.Message("&T/DeleteLvl [level]");
            p.Message("&HCompletely deletes [level] (portals, MBs, everything)");
            p.Message("&HA backup of the level is made in the levels/deleted folder");
        }
    }
}