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
using MAX.Events.GroupEvents;

namespace MAX.Orders.Moderation
{
    public class OrdSetRank : Order
    {
        public override string Name { get { return "SetRank"; } }
        public override string Shortcut { get { return "Rank"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get
            {
                return new[] { new OrderDesignation("pr", "+up"), new OrderDesignation("de", "-down"),
                    new OrderDesignation("Promote", "+up"), new OrderDesignation("Demote", "-down") };
            }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces(3);
            if (args.Length < 2) { Help(p); return; }
            string rankName, target;
            string reason = args.Length > 2 ? args[2] : null;

            if (args[0].CaselessEq("+up"))
            {
                rankName = args[0];
                target = ModActionOrd.FindName(p, "promote", "Promote", "", args[1], ref reason);
            }
            else if (args[0].CaselessEq("-down"))
            {
                rankName = args[0];
                target = ModActionOrd.FindName(p, "demote", "Demote", "", args[1], ref reason);
            }
            else
            {
                rankName = args[1];
                target = ModActionOrd.FindName(p, "rank", "Rank", " " + rankName, args[0], ref reason);
            }

            if (target == null) return;
            if (p.name.CaselessEq(target))
            {
                p.Message("Cannot change your own rank."); return;
            }

            Group curRank = PlayerInfo.GetGroup(target);
            Group newRank = TargetRank(p, rankName, curRank);
            if (newRank == null) return;

            if (curRank == newRank)
            {
                p.Message("{0} &Sis already ranked {1}",
                          p.FormatNick(target), curRank.ColoredName);
                return;
            }
            if (!CanChangeRank(target, curRank, newRank, p, data, ref reason)) return;

            ModAction action = new ModAction(target, p, ModActionType.Rank, reason)
            {
                targetGroup = curRank,
                Metadata = newRank
            };
            OnModActionEvent.Call(action);
        }

        public static bool CanChangeRank(string name, Group curRank, Group newRank,
                                           Player p, OrderData data, ref string reason)
        {
            Group banned = Group.BannedRank;
            if (reason == null)
            {
                reason = newRank.Permission >= curRank.Permission ?
                    Server.Config.DefaultPromoteMessage : Server.Config.DefaultDemoteMessage;
            }
            reason = ModActionOrd.ExpandReason(p, reason);
            if (reason == null) return false;

            if (newRank == banned)
            {
                p.Message("Use &T/Ban &Sto change a player's rank to {0}&S.", banned.ColoredName); return false;
            }
            if (curRank == banned)
            {
                p.Message("Use &T/Unban &Sto change a player's rank from &S{0}.", banned.ColoredName); return false;
            }

            if (!CheckRank(p, data, name, curRank.Permission, "change the rank of", false)) return false;
            if (newRank.Permission >= data.Rank)
            {
                p.Message("Cannot rank a player to a rank equal to or higher than yours."); return false;
            }

            if (newRank.Permission == curRank.Permission)
            {
                p.Message("{0} &Sis already ranked {1}.",
                          p.FormatNick(name), curRank.ColoredName); return false;
            }

            bool cancel = false;
            OnChangingGroupEvent.Call(name, curRank, newRank, ref cancel);
            return !cancel;
        }

        public static Group TargetRank(Player p, string name, Group curRank)
        {
            if (name.CaselessEq("+up")) return NextRankUp(p, curRank);
            if (name.CaselessEq("-down")) return NextRankDown(p, curRank);
            return Matcher.FindRanks(p, name);
        }

        public static Group NextRankDown(Player p, Group curRank)
        {
            int index = Group.GroupList.IndexOf(curRank);
            if (index > 0)
            {
                Group next = Group.GroupList[index - 1];
                if (next.Permission > LevelPermission.Banned) return next;
            }
            p.Message("No lower ranks exist"); return null;
        }

        public static Group NextRankUp(Player p, Group curRank)
        {
            int index = Group.GroupList.IndexOf(curRank);
            if (index < Group.GroupList.Count - 1)
            {
                Group next = Group.GroupList[index + 1];
                return next;
            }
            p.Message("No higher ranks exist"); return null;
        }

        public override void Help(Player p)
        {
            p.Message("&T/SetRank [player] [rank] <reason>");
            p.Message("&HSets that player's rank/group, with an optional reason.");
            p.Message("&HTo see available ranks, type &T/ViewRanks");
            p.Message("&HFor <reason>, @number can be used as a shortcut for that rule.");
        }
    }
}