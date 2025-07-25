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
using System;

namespace MAX.Orders.Moderation
{
    public class OrdTempBan : Order
    {
        public override string Name { get { return "TempBan"; } }
        public override string Shortcut { get { return "tb"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.AdvBuilder; } }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(3);
            string reason = args.Length > 2 ? args[2] : "";

            string target = ModActionOrd.FindName(p, "temp ban", "TempBan",
                                                 args.Length == 1 ? "" : " " + args[1],
                                                 args[0], ref reason);
            if (target == null) return;

            Group group = ModActionOrd.CheckTarget(p, data, "temp ban", target);
            if (group == null) return;

            if (Server.tempBans.Contains(target))
            {
                p.Message("{0} &Sis already temp-banned.", p.FormatNick(target));
                return;
            }

            TimeSpan span = TimeSpan.FromHours(1);
            if (args.Length > 1 && !OrderParser.GetTimespan(p, args[1], ref span, "temp ban for", "m")) return;
            if (span.TotalSeconds < 1) { p.Message("Cannot temp ban someone for less than a second."); return; }

            reason = ModActionOrd.ExpandReason(p, reason);
            if (reason == null) return;

            ModAction action = new ModAction(target, p, ModActionType.Ban, reason, span)
            {
                targetGroup = group
            };
            OnModActionEvent.Call(action);
        }

        public override void Help(Player p)
        {
            p.Message("&T/TempBan [name] [timespan] <reason>");
            p.Message("&HBans [name] for [timespan]. Default is 1 hour.");
            p.Message("&H e.g. to tempban for 90 minutes, [timespan] would be &S1h30m");
            p.Message("&HFor <reason>, @number can be used as a shortcut for that rule.");
        }
    }
}