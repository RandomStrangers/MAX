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
using MAX.Events;

namespace MAX.Orders.Moderation
{
    public class OrdUnban : Order
    {
        public override string Name { get { return "Unban"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(2);

            string reason = args.Length > 1 ? args[1] : "";
            reason = ModActionOrd.ExpandReason(p, reason);
            if (reason == null) return;

            if (!Server.tempBans.Contains(args[0]))
            {
                args[0] = Group.BannedRank.Players.FindMatches(p, args[0], "banned players", out int _);
                if (args[0] == null) return;
            }

            ModAction action = new ModAction(args[0], p, ModActionType.Unban, reason);
            OnModActionEvent.Call(action);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Unban [player] <reason>");
            p.Message("&HUnbans a player. This includes temporary bans.");
            p.Message("&HFor <reason>, @number can be used as a shortcut for that rule.");
        }
    }
}