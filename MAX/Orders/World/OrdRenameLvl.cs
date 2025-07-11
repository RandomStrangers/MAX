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
    public class OrdRenameLvl : Order
    {
        public override string Name { get { return "RenameLvl"; } }
        public override string Type { get { return OrderTypes.World; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Admin; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("WRename"), new OrderDesignation("WorldRename") }; }
        }
        public override bool MessageBlockRestricted { get { return true; } }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces();
            if (args.Length != 2) { Help(p); return; }

            string src = Matcher.FindMaps(p, args[0]);
            if (src == null) return;
            if (!LevelInfo.Check(p, data.Rank, src, "rename this map", out LevelConfig cfg)) return;

            string dst = args[1].ToLower();
            if (!Formatter.ValidMapName(p, dst)) return;

            if (!LevelActions.Rename(p, src, dst)) return;
            Chat.MessageGlobal("Level {0} &Swas renamed to {1}", cfg.Color + src, cfg.Color + dst);
        }

        public override void Help(Player p)
        {
            p.Message("&T/RenameLvl [level] [new name]");
            p.Message("&HRenames [level] to [new name]");
        }
    }
}