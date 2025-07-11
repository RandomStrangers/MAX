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
using MAX.Events;
using System;

namespace MAX.Orders.Moderation
{
    public class OrdMute : Order
    {
        public override string Name { get { return "Mute"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }

        public const string UNMUTE_FLAG = "-unmute";

        public override OrderDesignation[] Designations
        { get { return new[] { new OrderDesignation("Unmute", UNMUTE_FLAG) }; } }
        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(3);
            string target;

            if (args[0].CaselessEq(UNMUTE_FLAG))
            {
                if (args.Length == 1) { Help(p); return; }
                target = PlayerInfo.FindMatchesPreferOnline(p, args[1]);
                if (target == null) return;

                if (!Server.muted.Contains(target))
                {
                    p.Message("{0}&S is not muted.", p.FormatNick(target));
                    return;
                }

                DoUnmute(p, target, args.Length > 2 ? args[2] : "");
                return;
            }

            target = PlayerInfo.FindMatchesPreferOnline(p, args[0]);
            if (target == null) return;

            if (Server.muted.Contains(target))
            {
                p.Message("{0}&S is already muted.", p.FormatNick(target));
                p.Message("You may unmute them with &T/Unmute {0}", target);
            }
            else
            {
                Group group = ModActionOrd.CheckTarget(p, data, "mute", target);
                if (group == null) return;

                DoMute(p, target, args);
            }
        }

        public void DoMute(Player p, string target, string[] args)
        {
            TimeSpan duration = Server.Config.ChatSpamMuteTime;
            if (args.Length > 1)
            {
                if (!OrderParser.GetTimespan(p, args[1], ref duration, "mute for", "s")) return;
            }

            string reason = args.Length > 2 ? args[2] : "";
            reason = ModActionOrd.ExpandReason(p, reason);
            if (reason == null) return;

            ModAction action = new ModAction(target, p, ModActionType.Muted, reason, duration);
            OnModActionEvent.Call(action);
        }

        public void DoUnmute(Player p, string target, string reason)
        {
            reason = ModActionOrd.ExpandReason(p, reason);
            if (reason == null) return;
            if (p.name == target) { p.Message("You cannot unmute yourself."); return; }

            ModAction action = new ModAction(target, p, ModActionType.Unmuted, reason);
            OnModActionEvent.Call(action);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Mute [player] <timespan> <reason>");
            p.Message("&H Mutes player for <timespan>, which defaults to");
            p.Message("&H the auto-mute timespan.");
            p.Message("&H If <timespan> is 0, the mute is permanent.");
            p.Message("&H For <reason>, @1 substitutes for rule 1, @2 for rule 2, etc.");
            p.Message("&T/Unmute [player] <reason>");
            p.Message("&H Unmutes player with optional <reason>.");
        }
    }
}