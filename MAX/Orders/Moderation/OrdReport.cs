﻿/*
 * Written By Jack1312

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
using MAX.DB;
using MAX.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace MAX.Orders.Moderation
{
    public class OrdReport : Order
    {
        public override string Name { get { return "Report"; } }
        public override string Type { get { return OrderTypes.Moderation; } }
        public override OrderPerm[] ExtraPerms
        {
            get { return new[] { new OrderPerm(LevelPermission.Operator, "can manage reports") }; }
        }
        public override OrderDesignation[] Designations
        {
            get { return new OrderDesignation[] { new OrderDesignation("Reports", "list") }; }
        }

        public override void Use(Player p, string message, OrderData data)
        {
            if (message.Length == 0) { Help(p); return; }
            string[] args = message.SplitSpaces(2);
            if (!Directory.Exists("extra/reported"))
                Directory.CreateDirectory("extra/reported");

            string cmd = args[0];
            if (IsListOrder(cmd))
            {
                HandleList(p, args, data);
            }
            else if (cmd.CaselessEq("clear"))
            {
                HandleClear(p, data);
            }
            else if (IsDeleteOrder(cmd))
            {
                HandleDelete(p, args, data);
            }
            else if (IsInfoOrder(cmd))
            {
                HandleCheck(p, args, data);
            }
            else
            {
                HandleAdd(p, args);
            }
        }

        public void HandleList(Player p, string[] args, OrderData data)
        {
            if (!CheckExtraPerm(p, data, 1)) return;
            string[] users = GetReportedUsers();

            if (users.Length > 0)
            {
                p.Message("The following players have been reported:");
                string modifier = args.Length > 1 ? args[1] : "";
                Paginator.Output(p, users, pl => p.FormatNick(pl),
                                 "Review list", "players", modifier);

                p.Message("Use &T/Report check [player] &Sto view report details.");
                p.Message("Use &T/Report delete [player] &Sto delete a report");
            }
            else
            {
                p.Message("No players have been reported currently.");
            }
        }

        public void HandleCheck(Player p, string[] args, OrderData data)
        {
            if (args.Length != 2)
            {
                p.Message("You need to provide a player's name.");
                return;
            }
            if (!CheckExtraPerm(p, data, 1))
            {
                return;
            }

            string target = PlayerDB.MatchNames(p, args[1]);
            if (target == null)
            {
                return;
            }
            string nick = p.FormatNick(target);

            if (!HasReports(target))
            {
                p.Message("{0} &Shas not been reported.", nick);
                return;
            }

            string[] reports = File.ReadAllLines("extra/reported/" + target + ".txt");
            p.MessageLines(reports);
        }

        public void HandleDelete(Player p, string[] args, OrderData data)
        {
            if (args.Length != 2)
            {
                p.Message("You need to provide a player's name."); return;
            }
            if (!CheckExtraPerm(p, data, 1)) return;

            string target = PlayerDB.MatchNames(p, args[1]);
            if (target == null) return;
            string nick = p.FormatNick(target);

            if (!HasReports(target))
            {
                p.Message("{0} &Shas not been reported.", nick); return;
            }
            if (!Directory.Exists("extra/reportedbackups"))
                Directory.CreateDirectory("extra/reportedbackups");

            DeleteReport(target);
            p.Message("Reports on {0} &Swere deleted.", nick);
            Chat.MessageFromOps(p, "λNICK &Sdeleted reports on " + nick);
            Logger.Log(LogType.UserActivity, "Reports on {1} were deleted by {0}", p.name, target);
        }

        public void HandleClear(Player p, OrderData data)
        {
            if (!CheckExtraPerm(p, data, 1)) return;
            if (!Directory.Exists("extra/reportedbackups"))
                Directory.CreateDirectory("extra/reportedbackups");

            string[] users = GetReportedUsers();
            foreach (string user in users) { DeleteReport(user); }

            p.Message("&aYou have cleared all reports!");
            Chat.MessageFromOps(p, "λNICK &ccleared ALL reports!");
            Logger.Log(LogType.UserActivity, p.name + " cleared ALL reports!");
        }

        public void HandleAdd(Player p, string[] args)
        {
            if (args.Length != 2)
            {
                p.Message("You need to provide a reason for the report."); return;
            }

            string target = PlayerDB.MatchNames(p, args[0]);
            if (target == null) return;
            string nick = p.FormatNick(target);

            List<string> reports = new List<string>();
            if (HasReports(target))
            {
                reports = Utils.ReadAllLinesList(ReportPath(target));
            }
            ItemPerms checkPerms = Orders.OrderExtraPerms.Find(Name, 1);

            if (reports.Count >= 5)
            {
                p.Message("{0} &Walready has 5 reports! Please wait until an {1} &Whas reviewed these reports first!",
                          nick, Orders.OrderExtraPerms.Find(Name, 1).Describe());
                return;
            }

            string reason = ModActionOrd.ExpandReason(p, args[1]);
            if (reason == null) return;

            reports.Add(reason + " - Reported by " + p.name + " at " + DateTime.Now);
            File.WriteAllLines(ReportPath(target), reports.ToArray());
            p.Message("&aReport sent! It should be viewed when a {0} &ais online",
                      checkPerms.Describe());

            ModAction action = new ModAction(target, p, ModActionType.Reported, reason);
            OnModActionEvent.Call(action);
            if (!action.Announce) return;

            string opsMsg = "λNICK &Sreported " + nick + "&S. Reason: " + reason;
            Chat.MessageFrom(ChatScope.Perms, p, opsMsg, checkPerms, null, true);
            string allMsg = "Use &T/Report check " + target + " &Sto see all of " + Pronouns.GetFor(target)[0].Object + " reports";
            Chat.MessageFrom(ChatScope.Perms, p, allMsg, checkPerms, null, true);
        }


        public static bool HasReports(string user)
        {
            return File.Exists(ReportPath(user));
        }
        public static string ReportPath(string user)
        {
            return "extra/reported/" + user + ".txt";
        }

        public static string[] GetReportedUsers()
        {
            string[] users = FileIO.TryGetFiles("extra/reported", "*.txt");
            for (int i = 0; i < users.Length; i++)
            {
                users[i] = Path.GetFileNameWithoutExtension(users[i]);
            }
            return users;
        }

        public static void DeleteReport(string user)
        {
            string backup = "extra/reportedbackups/" + user + ".txt";
            FileIO.TryDelete(backup);
            FileIO.TryMove(ReportPath(user), backup);
        }

        public override void Help(Player p)
        {
            p.Message("&T/Report list &H- Lists all reported players.");
            p.Message("&T/Report check [player] &H- Views reports for that player.");
            p.Message("&T/Report delete [player] &H- Deletes reports for that player.");
            p.Message("&T/Report clear &H- Clears &call&H reports.");
            p.Message("&T/Report [player] [reason] &H- Reports that player for the given reason.");
        }
    }
}
