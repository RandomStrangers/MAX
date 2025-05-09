﻿/*
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
using MAX.Events.PlayerEvents;

namespace MAX.Orders.Moderation {
    public sealed class OrdHide : Order2 {
        public override string name { get { return "Hide"; } }
        public override string type { get { return OrderTypes.Moderation; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override bool SuperUseable { get { return false; } }
        public override bool UpdatesLastOrd { get { return false; } }
        public override OrderPerm[] ExtraPerms {
            get { return new[] { new OrderPerm(LevelPermission.Admin, "can hide silently") }; }
        }
        public override OrderDesignation[] Designations {
            get { return new OrderDesignation[] { new OrderDesignation("XHide", "silent") }; }
        }

        public static void AnnounceOps(Player p, string msg) {
            ItemPerms perms = new ItemPerms(p.hideRank);
            Chat.MessageFrom(ChatScope.Perms, p, msg, perms, null, true);
        }

        public override void Use(Player p, string message, OrderData data) {
            if (message.Length > 0 && p.possess.Length > 0) {
                p.Message("Stop your current possession first."); return;
            }
            bool silent = false;
            if (message.CaselessEq("silent")) {
                if (!CheckExtraPerm(p, data, 1)) return;
                silent = true;
            }
            
            Order adminchat = Find("AdminChat");
            Order opchat = Find("OpChat");
            Entities.GlobalDespawn(p, false);
            
            p.hidden = !p.hidden;
            if (p.hidden) {
                p.hideRank = data.Rank;
                AnnounceOps(p, "To Ops -λNICK&S- is now &finvisible");               
                
                if (!silent) {
                    string leaveMsg = "&c- λFULL &S" + PlayerInfo.GetLogoutMessage(p);
                    Chat.MessageFrom(ChatScope.All, p, leaveMsg, null, null, true);
                }
                
                if (!p.opchat) opchat.Use(p, "", data);
                Server.hidden.Add(p.name);
                OnPlayerActionEvent.Call(p, PlayerAction.Hide);
            } else {
                AnnounceOps(p, "To Ops -λNICK&S- is now &fvisible");
                p.hideRank = LevelPermission.Banned;
                
                if (!silent) {
                    string joinMsg = "&a+ λFULL &S" + PlayerInfo.GetLoginMessage(p);
                    Chat.MessageFrom(ChatScope.All, p, joinMsg, null, null, true);
                }
                
                if (p.opchat) opchat.Use(p, "", data);
                if (p.adminchat) adminchat.Use(p, "", data);
                Server.hidden.Remove(p.name);
                OnPlayerActionEvent.Call(p, PlayerAction.Unhide);
            }
            
            Entities.GlobalSpawn(p, false);
            TabList.Add(p, p, Entities.SelfID);
            Server.hidden.Save(false);
        }

        public override void Help(Player p) {
            p.Message("&T/Hide &H- Toggles your visibility to other players, also toggles opchat.");
            p.Message("&T/Hide silent &H- Hides without showing join/leave message");
            p.Message("&HUse &T/OHide &Hto hide other players.");
        }
    }
}