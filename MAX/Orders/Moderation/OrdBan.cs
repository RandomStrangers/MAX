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
    public class OrdBan : Order
    {
        public override string Name { get { return "Ban"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("KickBan"), new OrderDesignation("kb") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }

            string[] args = message.SplitSpaces(2);
            string reason = args.Length > 1 ? args[1] : "";
            string target = ModActionOrd.FindName(p, "ban", "Ban", "", args[0], ref reason);
            if (target == null) return;

            reason = ModActionOrd.ExpandReason(p, reason);
            if (reason == null) return;

            Group group = ModActionOrd.CheckTarget(p, data, "ban", target);
            if (group == null) return;

            if (group.Permission == LevelPermission.Banned)
            {
                p.Message("{0} &Sis already banned.", p.FormatNick(target));
                return;
            }

            ModAction action = new ModAction(target, p, ModActionType.Ban, reason)
            {
                targetGroup = group
            };
            OnModActionEvent.Call(action);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Ban [player] <reason>");
            p.Message("&HBans a player (and kicks them if online).");
            p.Message("&HFor <reason>, @number can be used as a shortcut for that rule.");
        }
    }
}