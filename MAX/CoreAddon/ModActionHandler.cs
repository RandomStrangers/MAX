﻿/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCForge)
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
using MAX.DB;
using MAX.Events;
using MAX.Orders;
using MAX.Orders.Moderation;
using MAX.Tasks;
using System;

namespace MAX.Core
{
    public static class ModActionHandler
    {

        public static void HandleModAction(ModAction action)
        {
            switch (action.Type)
            {
                case ModActionType.Jailed: DoJail(action); break;
                case ModActionType.Unjailed: DoUnjail(action); break;
                case ModActionType.Muted: DoMute(action); break;
                case ModActionType.Unmuted: DoUnmute(action); break;
                case ModActionType.Ban: DoBan(action); break;
                case ModActionType.Unban: DoUnban(action); break;
                case ModActionType.BanIP: DoBanIP(action); break;
                case ModActionType.UnbanIP: DoUnbanIP(action); break;
                case ModActionType.Warned: DoWarn(action); break;
                case ModActionType.Rank: DoRank(action); break;
            }
        }

        public static void LogAction(ModAction e, string action)
        {
            // TODO should use per-player nick settings
            string targetNick = e.Actor.FormatNick(e.Target);

            if (e.Announce)
            {
                // TODO: Chat.MessageFrom if target is online?
                Player who = PlayerInfo.FindExact(e.Target);
                // TODO: who.SharesChatWith
                Chat.Message(ChatScope.Global, e.FormatMessage(targetNick, action),
                             null, null, true);
            }
            else
            {
                Chat.MessageOps(e.FormatMessage(targetNick, action));
            }

            action = Colors.StripUsed(action);
            string suffix = "";
            if (e.Duration.Ticks != 0) suffix = " &Sfor " + e.Duration.Shorten();

            Logger.Log(LogType.UserActivity, "{0} was {1} by {2}",
                       e.Target, action, e.Actor.name + suffix);
        }


        public static void DoJail(ModAction e)
        {
            Player who = PlayerInfo.FindExact(e.Target);
            if (who != null) who.jailed = true;
            LogAction(e, "&7jailed");

            Server.jailed.Update(e.Target, FormatModTaskData(e));
            ModerationTasks.JailCalcNextRun();
            Server.jailed.Save();
        }

        public static void DoUnjail(ModAction e)
        {
            Player who = PlayerInfo.FindExact(e.Target);
            if (who != null) who.jailed = false;
            LogAction(e, "&aunjailed");

            Server.jailed.Remove(e.Target);
            ModerationTasks.JailCalcNextRun();
            Server.jailed.Save();
        }


        public static void DoMute(ModAction e)
        {
            Player who = PlayerInfo.FindExact(e.Target);
            if (who != null) who.muted = true;
            LogAction(e, "&8muted");

            Server.muted.Update(e.Target, FormatModTaskData(e));
            ModerationTasks.MuteCalcNextRun();
            Server.muted.Save();
        }

        public static void DoUnmute(ModAction e)
        {
            Player who = PlayerInfo.FindExact(e.Target);
            if (who != null) who.muted = false;
            LogAction(e, "&aun-muted");

            Server.muted.Remove(e.Target);
            ModerationTasks.MuteCalcNextRun();
            Server.muted.Save();
        }


        public static void DoBan(ModAction e)
        {
            Player who = PlayerInfo.FindExact(e.Target);
            LogAction(e, "&8banned");

            if (e.Duration.Ticks != 0)
            {
                DateTime end = DateTime.UtcNow.Add(e.Duration);
                string data = Ban.PackTempBanData(e.Reason, e.Actor.name, end);

                Server.tempBans.Update(e.Target, data);
                Server.tempBans.Save();

                who?.Kick("Banned for " + e.Duration.Shorten(true) + "." + e.ReasonSuffixed);
            }
            else
            {
                Ban.DeleteBan(e.Target);
                Ban.BanPlayer(e.Actor, e.Target, e.Reason, !e.Announce, e.TargetGroup.Name);
                ModActionOrd.ChangeRank(e.Target, e.targetGroup, Group.BannedRank, who);

                if (who != null)
                {
                    string msg = e.Reason.Length == 0 ? Server.Config.DefaultBanMessage : e.Reason;
                    who.Kick("Banned by " + e.Actor.ColoredName + ": &S" + msg,
                             "Banned by " + e.Actor.ColoredName + ": &f" + msg);
                }
            }
        }

        public static void DoUnban(ModAction e)
        {
            Player who = PlayerInfo.FindExact(e.Target);
            LogAction(e, "&8unbanned");

            if (Server.tempBans.Remove(e.Target))
            {
                Server.tempBans.Save();
            }
            if (!Group.BannedRank.Players.Contains(e.Target)) return;

            Ban.DeleteUnban(e.Target);
            Ban.UnbanPlayer(e.Actor, e.Target, e.Reason);
            ModActionOrd.ChangeRank(e.Target, Group.BannedRank, Group.DefaultRank, who, false);

            string ip = PlayerDB.FindIP(e.Target);
            if (ip != null && Server.bannedIP.Contains(ip))
            {
                e.Actor.Message("NOTE: {0} IP is still banned.", Pronouns.GetFor(e.Target)[0].Object);
            }
        }


        public static void LogIPAction(ModAction e, string type)
        {
            ItemPerms perms = OrderExtraPerms.Find("WhoIs", 1);
            Chat.Message(ChatScope.Global, e.FormatMessage("An IP", type), perms,
                         FilterNotItemPerms, true);
            Chat.Message(ChatScope.Global, e.FormatMessage(e.Target, type), perms,
                         Chat.FilterPerms, true);
        }

        public static bool FilterNotItemPerms(Player pl, object arg)
        {
            return !Chat.FilterPerms(pl, arg);
        }

        public static void DoBanIP(ModAction e)
        {
            LogIPAction(e, "&8IP banned");
            Logger.Log(LogType.UserActivity, "IP-BANNED: {0} by {1}.{2}",
                       e.Target, e.Actor.name, e.ReasonSuffixed);
            Server.bannedIP.Update(e.Target, e.Reason);
            Server.bannedIP.Save();
        }

        public static void DoUnbanIP(ModAction e)
        {
            LogIPAction(e, "&8IP unbanned");
            Logger.Log(LogType.UserActivity, "IP-UNBANNED: {0} by {1}.",
                       e.Target, e.Actor.name);
            Server.bannedIP.Remove(e.Target);
            Server.bannedIP.Save();
        }


        public static void DoWarn(ModAction e)
        {
            Player who = PlayerInfo.FindExact(e.Target);
            if (who != null)
            {
                LogAction(e, "&ewarned");
                if (who.warn == 0)
                {
                    who.Message("Do it again twice and you will get kicked!");
                }
                else if (who.warn == 1)
                {
                    who.Message("Do it one more time and you will get kicked!");
                }
                else if (who.warn == 2)
                {
                    Chat.MessageGlobal("{0} &Swas warn-kicked by {1}", who.ColoredName, e.Actor.ColoredName);
                    string chatMsg = "by " + e.Actor.ColoredName + "&S: " + e.Reason;
                    string kickMsg = "Kicked by " + e.Actor.ColoredName + ": &f" + e.Reason;
                    who.Kick(chatMsg, kickMsg);
                }
                who.warn++;
            }
            else
            {
                if (!Server.Config.LogNotes)
                {
                    e.Actor.Message("Notes logging must be enabled to warn offline players."); return;
                }
                LogAction(e, "&ewarned");
            }
        }


        public static void DoRank(ModAction e)
        {
            Player who = PlayerInfo.FindExact(e.Target);
            Group newRank = (Group)e.Metadata;
            string action = newRank.Permission >= e.TargetGroup.Permission ? "promoted to " : "demoted to ";
            LogAction(e, action + newRank.ColoredName);

            if (who != null && e.Announce)
            {
                who.Message("You are now ranked " + newRank.ColoredName + "&S, type /Help for your new set of orders.");
            }
            if (Server.tempRanks.Remove(e.Target))
            {
                ModerationTasks.TemprankCalcNextRun();
                Server.tempRanks.Save();
            }

            WriteRankInfo(e, newRank);
            if (e.Duration != TimeSpan.Zero) AddTempRank(e, newRank);
            ModActionOrd.ChangeRank(e.Target, e.TargetGroup, newRank, who);
        }

        public static void WriteRankInfo(ModAction e, Group newRank)
        {
            string assigner = e.Actor.name;
            long time = DateTime.UtcNow.ToUnixTime();

            string line = e.Target + " " + assigner + " " + time + " " + newRank.Name
                + " " + e.TargetGroup.Name + " " + e.Reason.Replace(" ", "%20");
            Server.RankInfo.Append(line);
        }

        public static void AddTempRank(ModAction e, Group newRank)
        {
            string data = FormatModTaskData(e) + " " + e.TargetGroup.Name + " " + newRank.Name;
            Server.tempRanks.Update(e.Target, data);
            ModerationTasks.TemprankCalcNextRun();
            Server.tempRanks.Save();
        }

        public static string FormatModTaskData(ModAction e)
        {
            long assign = DateTime.UtcNow.ToUnixTime();
            DateTime end = DateTime.MaxValue.AddYears(-1);

            if (e.Duration != TimeSpan.Zero)
            {
                try
                {
                    end = DateTime.UtcNow.Add(e.Duration);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // user provided extreme expiry time, ignore it
                }
            }

            long expiry = end.ToUnixTime();
            string assigner = e.Actor.name;
            return assigner + " " + assign + " " + expiry;
        }
    }
}