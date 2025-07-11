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
using System;

namespace MAX.Orders.Chatting
{
    public class OrdAfk : Order
    {
        public override string Name { get { return "AFK"; } }
        public override string Type { get { return OrderTypes.Information; } }
        public override bool SuperUseable { get { return false; } }
        public override bool MessageBlockRestricted { get { return true; } }
        public override bool UseableWhenJailed { get { return true; } }

        public override void Use(Player p, string message, OrderData data) { ToggleAfk(p, message); }
        public static void ToggleAfk(Player p, string message)
        {
            if (p.joker) message = "";
            p.AutoAfk = false;
            p.IsAfk = !p.IsAfk;
            p.afkMessage = p.IsAfk ? message : null;
            TabList.Update(p, true);
            p.LastAction = DateTime.UtcNow;

            bool cantSend = !p.CanSpeak();
            if (p.IsAfk)
            {
                if (cantSend)
                {
                    p.Message("You are now marked as being AFK.");
                }
                else
                {
                    ShowMessage(p, "-λNICK&S- is AFK " + message);
                    p.CheckForMessageSpam();
                }
                p.AFKCooldown = DateTime.UtcNow.AddSeconds(2);
                OnPlayerActionEvent.Call(p, PlayerAction.AFK, null, cantSend);
            }
            else
            {
                if (cantSend)
                {
                    p.Message("You are no longer marked as being AFK.");
                }
                else
                {
                    ShowMessage(p, "-λNICK&S- is no longer AFK");
                    p.CheckForMessageSpam();
                }
                OnPlayerActionEvent.Call(p, PlayerAction.UnAFK, null, cantSend);
            }
        }

        public static void ShowMessage(Player p, string message)
        {
            bool announce = !p.hidden && Server.Config.IRCShowAFK;
            Chat.MessageFrom(p, message, Chat.FilterVisible(p), announce);
        }

        public override void Help(Player p)
        {
            p.Message("&T/AFK <reason>");
            p.Message("&HMarks yourself as AFK. Use again to mark yourself as back");
        }
    }
}