/*
    Copyright 2015 MCGalaxy
    
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
using System;
using System.Collections.Generic;
using MAX.Orders.Moderation;
using MAX.Events;

namespace MAX.Tasks {
    public static class ModerationTasks {

        public static SchedulerTask temprankTask, jailTask, muteTask;
        public static void QueueTasks() {
            temprankTask = Server.MainScheduler.QueueRepeat(
                TemprankCheckTask, null, NextRun(Server.tempRanks));
            jailTask = Server.MainScheduler.QueueRepeat(
                JailCheckTask, null, NextRun(Server.jailed));
            muteTask = Server.MainScheduler.QueueRepeat(
                MuteCheckTask, null, NextRun(Server.muted));
        }


        public static void TemprankCheckTask(SchedulerTask task) {
            DoTask(task, Server.tempRanks, TemprankCallback);
        }

        public static void TemprankCalcNextRun() { CalcNextRun(temprankTask, Server.tempRanks); }

        public static void TemprankCallback(string[] args) {
            OrdTempRank.Delete(Player.MAX, args[0], Player.MAX.DefaultOrdData);
            // Handle case of old rank no longer existing
            if (Server.tempRanks.Remove(args[0])) {
                Server.tempRanks.Save();
            }
        }


        public static void JailCheckTask(SchedulerTask task) {
            DoTask(task, Server.jailed, JailCallback);
        }

        public static void JailCalcNextRun() { CalcNextRun(jailTask, Server.jailed); }

        public static void JailCallback(string[] args) {
            ModAction action = new ModAction(args[0], Player.MAX, ModActionType.Unjailed, "auto unjail");
            OnModActionEvent.Call(action);
        }


        public static void MuteCheckTask(SchedulerTask task) {
            DoTask(task, Server.muted, MuteCallback);
        }

        public static void MuteCalcNextRun() { CalcNextRun(muteTask, Server.muted); }

        public static void MuteCallback(string[] args) {
            ModAction action = new ModAction(args[0], Player.MAX, ModActionType.Unmuted, "auto unmute");
            OnModActionEvent.Call(action);
        }


        public static void DoTask(SchedulerTask task, PlayerExtList list, Action<string[]> callback) {
            List<string> lines = list.AllLines();
            foreach (string line in lines) {
                string[] args = line.SplitSpaces();
                if (args.Length < 4) continue;

                int expiry;
                if (!int.TryParse(args[3], out expiry)) continue;
                if (DateTime.UtcNow < expiry.FromUnixTime()) continue;
                
                callback(args);
            }
            task.Delay = NextRun(list);
        }

        public static void CalcNextRun(SchedulerTask task, PlayerExtList list) {
            task.Delay = NextRun(list);
            task.NextRun = DateTime.UtcNow.Add(task.Delay);
            Server.MainScheduler.Recheck();
        }

        public static TimeSpan NextRun(PlayerExtList list) {
            DateTime nextRun = DateTime.MaxValue.AddYears(-1);
            // Lock because we want to ensure list not modified from under us
            lock (list.locker) {
                List<string> lines = list.AllLines();
                // Line format: name assigner assigntime expiretime [whatever other data, we don't care]
                
                foreach (string line in lines) {
                    string[] args = line.SplitSpaces();
                    if (args.Length < 4) continue;

                    int expiry;
                    if (!int.TryParse(args[3], out expiry)) continue;
                    
                    DateTime expireTime = expiry.FromUnixTime();
                    if (expireTime < nextRun)
                        nextRun = expireTime;
                }
            }
            return nextRun - DateTime.UtcNow;
        }
    }
}