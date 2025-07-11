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
using System.Collections.Generic;

namespace MAX.Orders.Moderation
{
    public class OrdTempRank : Order
    {
        public override string Name { get { return "TempRank"; } }
        public override string Shortcut { get { return "tr"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override LevelPermission DefaultRank { get { return LevelPermission.Operator; } }
        public override OrderDesignation[] Designations
        {
            get { return new[] { new OrderDesignation("dtr", "delete"), new OrderDesignation("trl", "list") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            string[] args = message.SplitSpaces(4);
            string ord = args[0];

            if (args.Length >= 3)
            {
                Assign(p, args, data);
            }
            else if (IsListOrder(ord))
            {
                List(p);
            }
            else if (IsDeleteOrder(ord) && args.Length > 1)
            {
                Delete(p, args[1], data);
            }
            else if (IsInfoOrder(ord) && args.Length > 1)
            {
                Info(p, args[1]);
            }
            else
            {
                Help(p);
            }
        }

        public static void Assign(Player p, string[] args, OrderData data)
        {
            string target = PlayerInfo.FindMatchesPreferOnline(p, args[0]);
            if (target == null) return;

            Group newRank = Matcher.FindRanks(p, args[1]);
            if (newRank == null) return;
            TimeSpan duration = TimeSpan.Zero;
            if (!OrderParser.GetTimespan(p, args[2], ref duration, "temp rank for", "h")) return;

            if (Server.tempRanks.Contains(target))
            {
                p.Message("&WThe player already has a temporary rank assigned!"); return;
            }

            if (p.name.CaselessEq(target))
            {
                p.Message("&WYou cannot assign yourself a temporary rank."); return;
            }

            Group curRank = PlayerInfo.GetGroup(target);
            string reason = args.Length > 3 ? args[3] : "assigning temp rank";
            if (!OrdSetRank.CanChangeRank(target, curRank, newRank, p, data, ref reason)) return;

            ModAction action = new ModAction(target, p, ModActionType.Rank, reason, duration)
            {
                targetGroup = curRank,
                Metadata = newRank
            };
            OnModActionEvent.Call(action);
        }

        public static void Delete(Player p, string target, OrderData data)
        {
            string line = Server.tempRanks.Get(target);
            if (string.IsNullOrEmpty(line))
            {
                p.Message("{0} &Whas not been assigned a temp rank.", p.FormatNick(target));
                return;
            }

            string[] parts = line.SplitSpaces();
            Group curRank = PlayerInfo.GetGroup(target);

            Group oldRank = Group.Find(parts[4 - 1]); // -1 because data, not whole line
            if (oldRank == null) return;

            string reason = "temp rank unassigned";
            if (!OrdSetRank.CanChangeRank(target, curRank, oldRank, p, data, ref reason)) return;

            ModAction action = new ModAction(target, p, ModActionType.Rank, reason)
            {
                Metadata = oldRank,
                targetGroup = curRank
            };
            OnModActionEvent.Call(action);
        }

        public static void Info(Player p, string target)
        {
            string data = Server.tempRanks.Get(target);
            if (string.IsNullOrEmpty(data))
            {
                p.Message("{0} &Whas not been assigned a temp rank.", p.FormatNick(target));
            }
            else
            {
                PrintTempRankInfo(p, target, data);
            }
        }

        public static void List(Player p)
        {
            List<string> lines = Server.tempRanks.AllLines();
            if (lines.Count == 0)
            {
                p.Message("&WThere are no players with a temporary rank assigned.");
            }
            else
            {
                p.Message("&ePlayers with a temporary rank assigned:");
                foreach (string line in lines)
                {
                    string[] bits = line.SplitSpaces(2);
                    PrintTempRankInfo(p, bits[0], bits[1]);
                }
            }
        }

        public static void PrintTempRankInfo(Player p, string name, string data)
        {
            string[] args = data.SplitSpaces();
            if (args.Length < 4) return;

            string assigner = args[0];
            DateTime assigned = int.Parse(args[1]).FromUnixTime();
            DateTime expiry = int.Parse(args[2]).FromUnixTime();
            string oldRank = Group.GetColoredName(args[3]);
            string tempRank = Group.GetColoredName(args[4]);

            TimeSpan assignDelta = DateTime.UtcNow - assigned;
            TimeSpan expireDelta = expiry - DateTime.UtcNow;
            p.Message("Temp rank information for {0}:", p.FormatNick(name));
            p.Message("  From {0} &Sto {1}&S, by {2} &a{3} &Sago, expires in &a{4}",
                           oldRank, tempRank, assigner,
                           assignDelta.Shorten(), expireDelta.Shorten());
        }

        public override void Help(Player p)
        {
            p.Message("&T/TempRank [player] [rank] [timespan] <reason>");
            p.Message("&HSets a temporary rank for the specified player.");
            p.Message("&T/TempRank info [player]");
            p.Message("&HLists information about the temp rank for the given player.");
            p.Message("&T/TempRank delete [player] &H- Removes player's temp rank.");
            p.Message("&T/TempRank list &H- Lists all current temp ranks.");
        }
    }
}