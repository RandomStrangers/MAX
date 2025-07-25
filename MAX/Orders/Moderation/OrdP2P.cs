/*
 * Written by Jack1312
 * 
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
namespace MAX.Orders.Moderation
{

    public class OrdP2P : Order
    {
        public override string Name { get { return "P2P"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override bool MuseumUsable { get { return false; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces();
            if (args.Length > 2) { Help(p); return; }
            if (args.Length == 1) { p.Message("You did not specify the target player."); return; }
            Player source = PlayerInfo.FindMatches(p, args[0]);
            Player target = PlayerInfo.FindMatches(p, args[1]);

            if (source == null || target == null) return;
            if (!CheckRank(p, data, source, "teleport", true)) return;

            p.Message("Attempting to teleport " + source.name + " to " + target.name + ".");
            Find("TP").Use(source, target.name, data);
        }

        public override void Help(Player p)
        {
            p.Message("&T/P2P [source] [target]");
            p.Message("&HTeleports the source player to the target player.");
        }
    }
}